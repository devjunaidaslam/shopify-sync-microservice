using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    /// <summary>
    /// Repository interface for Shopify transaction history operations
    /// </summary>
    public interface ITransactionHistoryRepository
    {
        /// <summary>
        /// Creates a new transaction history record
        /// </summary>
        /// <param name="transaction">Transaction history entity to create</param>
        /// <returns>Created transaction history entity</returns>
        Task<ShopifyTransactionHistory> CreateAsync(ShopifyTransactionHistory transaction);

        /// <summary>
        /// Gets transaction history records with search and pagination.
        /// Search filters by ProductId, TransactionType, EventType, and Status using case-insensitive matching (ILike).
        /// Returns both the total count and the paginated data in a single database call.
        /// </summary>
        /// <param name="filter">Filter and pagination options</param>
        /// <returns>Tuple containing total count and list of transaction history records</returns>
        Task<(int TotalCount, IEnumerable<ShopifyTransactionHistory> Data)> GetTransactionsAsync(TransactionFilterDto filter);

        /// <summary>
        /// Deletes old transaction history records based on retention policy
        /// </summary>
        /// <param name="retentionDays">Number of days to retain records</param>
        /// <returns>Number of records deleted</returns>
        Task<int> DeleteOldRecordsAsync(int retentionDays);
    }
}
