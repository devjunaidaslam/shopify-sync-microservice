using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    /// <summary>
    /// Repository implementation for Shopify transaction history operations
    /// </summary>
    public class TransactionHistoryRepository : ITransactionHistoryRepository
    {
        private readonly PartFinderDbContext _context;
        private readonly ILogger<TransactionHistoryRepository> _logger;

        public TransactionHistoryRepository(PartFinderDbContext context, ILogger<TransactionHistoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ShopifyTransactionHistory> CreateAsync(ShopifyTransactionHistory transaction)
        {
            try
            {
                _context.ShopifyTransactionHistories.Add(transaction);
                await _context.SaveChangesAsync();
                _logger.LogDebug("[TransactionHistoryRepository] Created transaction history record with ID: {Id}", transaction.Id);
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryRepository] Error creating transaction history record");
                throw;
            }
        }
        public async Task<(int TotalCount, IEnumerable<ShopifyTransactionHistory> Data)> GetTransactionsAsync(TransactionFilterDto filter)
        {
            try
            {
                var query = _context.ShopifyTransactionHistories.AsQueryable();
                
                // Apply search filter
                if (!string.IsNullOrEmpty(filter.search))
                {
                    string searchTerm = filter.search;
                    query = query.Where(t =>
                        EF.Functions.ILike(t.ProductId ?? "", $"%{searchTerm}%") ||
                        EF.Functions.ILike(t.TransactionType, $"%{searchTerm}%") ||
                        EF.Functions.ILike(t.EventType, $"%{searchTerm}%") ||
                        EF.Functions.ILike(t.Status, $"%{searchTerm}%")
                    );
                }
                
                // Get total count before pagination
                var totalCount = await query.CountAsync();
                
                // Apply sorting
                query = query.OrderByDescending(t => t.CreatedAt);
                
                // Apply pagination
                if (filter.page_size > 0)
                {
                    query = query.Skip((filter.page_no - 1) * filter.page_size)
                               .Take(filter.page_size);
                }
                
                var data = await query.ToListAsync();
                
                return (totalCount, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryRepository] Error getting transaction history records");
                throw;
            }
        }

        public async Task<int> DeleteOldRecordsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var oldRecordDeleted = await _context.ShopifyTransactionHistories
                    .Where(t => t.CreatedAt < cutoffDate).ExecuteDeleteAsync(); 

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[TransactionHistoryRepository] Deleted {Count} old transaction history records older than {RetentionDays} days", 
                        oldRecordDeleted, retentionDays);
                

                return oldRecordDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryRepository] Error deleting old transaction history records");
                throw;
            }
        }

    }
}
