using Microsoft.Extensions.Logging;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Service implementation for Shopify transaction history operations
    /// </summary>
    public class TransactionHistoryService : ITransactionHistoryService
    {
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;
        private readonly ICommonService _commonService;
        private readonly ILogger<TransactionHistoryService> _logger;
        private readonly ResponseMessageList _apiResponseMessageList = new();

        public TransactionHistoryService(
            ITransactionHistoryRepository transactionHistoryRepository,
            ICommonService commonService,
            ILogger<TransactionHistoryService> logger)
        {
            _transactionHistoryRepository = transactionHistoryRepository;
            _commonService = commonService;
            _logger = logger;
        }

        public async Task<Response> LogTransactionAsync(CreateTransactionDto transaction)
        {
            try
            {
                _logger.LogInformation("[TransactionHistoryService] Logging transaction for product: {ProductId}, event: {EventType}, microservice: {Microservice}", 
                    transaction.ProductId, transaction.EventType, transaction.Microservice);

                var entity = new ShopifyTransactionHistory
                {
                    ProductId = transaction.ProductId,
                    TransactionType = transaction.TransactionType,
                    EventType = transaction.EventType,
                    Status = transaction.Status,
                    Payload = transaction.Payload,
                    ErrorMessage = transaction.ErrorMessage,
                    SentAt = transaction.SentAt,
                    ReceivedBy = transaction.ReceivedBy,
                    CreatedAt = DateTime.UtcNow
                };

                var createdTransaction = await _transactionHistoryRepository.CreateAsync(entity);

                _logger.LogDebug("[TransactionHistoryService] Successfully logged transaction with ID: {Id}", createdTransaction.Id);

                return ResponseHelper.Success("Transaction logged successfully", createdTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryService] Error logging transaction");
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "LogTransactionAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> GetTransactionsAsync(TransactionFilterDto filter)
        {
            try
            {
                _logger.LogInformation("[TransactionHistoryService] Getting transactions with filters");

                // Single database call that returns both count and data
                var result = await _transactionHistoryRepository.GetTransactionsAsync(filter);
                var count = result.TotalCount;
                var transactions = result.Data;

                int totalPages = filter.page_size > 0 ? (int)Math.Ceiling((double)count / filter.page_size) : 0;
                var pagination = new PaginationInfo
                {
                    TotalItemCount = count,
                    PageNo = filter.page_no,
                    PerPage = filter.page_size,
                    TotalPages = totalPages,
                    NextPage = filter.page_no < totalPages ? filter.page_no + 1 : 0,
                    PrevPage = filter.page_no > 1 ? filter.page_no - 1 : 0
                };

                _logger.LogInformation("[TransactionHistoryService] Successfully retrieved {Count} transactions out of {TotalCount} total records", 
                    transactions.Count(), count);

                return ResponseHelper.Success("Transactions retrieved successfully", transactions , null, pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryService] Error getting transactions");
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetTransactionsAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage);
            }
        }

       
        public async Task<Response> CleanupOldRecordsAsync(int retentionDays)
        {
            try
            {
                _logger.LogInformation("[TransactionHistoryService] Cleaning up records older than {RetentionDays} days", retentionDays);

                var deletedCount = await _transactionHistoryRepository.DeleteOldRecordsAsync(retentionDays);

                return ResponseHelper.Success($"Successfully cleaned up {deletedCount} old records", new { DeletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryService] Error cleaning up old records");
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CleanupOldRecordsAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> LogSuccessTransactionAsync(string? productId, string eventType, string microservice, 
            string? payload = null, string? response = null, Dictionary<string, object>? additionalData = null)
        {
            try
            {
                var transaction = new CreateTransactionDto
                {
                    ProductId = productId,
                    TransactionType = "import",
                    EventType = eventType,
                    Status = "success",
                    Microservice = microservice,
                    Payload = payload,
                    Response = response,
                    SentAt = DateTime.UtcNow,
                    SentToShopifyAt = DateTime.UtcNow,
                    ResponseReceivedAt = DateTime.UtcNow
                };

                // Add additional data if provided
                if (additionalData != null)
                {
                    if (additionalData.ContainsKey("VariantId"))
                        transaction.VariantId = additionalData["VariantId"]?.ToString();
                    if (additionalData.ContainsKey("CollectionId"))
                        transaction.CollectionId = additionalData["CollectionId"]?.ToString();
                    if (additionalData.ContainsKey("RequestId"))
                        transaction.RequestId = additionalData["RequestId"]?.ToString();
                    if (additionalData.ContainsKey("HttpMethod"))
                        transaction.HttpMethod = additionalData["HttpMethod"]?.ToString();
                    if (additionalData.ContainsKey("Endpoint"))
                        transaction.Endpoint = additionalData["Endpoint"]?.ToString();
                    if (additionalData.ContainsKey("HttpStatusCode"))
                        transaction.HttpStatusCode = Convert.ToInt32(additionalData["HttpStatusCode"]);
                }

                return await LogTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryService] Error logging success transaction");
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "LogSuccessTransactionAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> LogFailedTransactionAsync(string? productId, string eventType, string microservice, 
            string errorMessage, string? payload = null, Dictionary<string, object>? additionalData = null)
        {
            try
            {
                var transaction = new CreateTransactionDto
                {
                    ProductId = productId,
                    TransactionType = "import",
                    EventType = eventType,
                    Status = "failed",
                    Microservice = microservice,
                    Payload = payload,
                    ErrorMessage = errorMessage,
                    SentAt = DateTime.UtcNow,
                    SentToShopifyAt = DateTime.UtcNow
                };

                // Add additional data if provided
                if (additionalData != null)
                {
                    if (additionalData.ContainsKey("VariantId"))
                        transaction.VariantId = additionalData["VariantId"]?.ToString();
                    if (additionalData.ContainsKey("CollectionId"))
                        transaction.CollectionId = additionalData["CollectionId"]?.ToString();
                    if (additionalData.ContainsKey("RequestId"))
                        transaction.RequestId = additionalData["RequestId"]?.ToString();
                    if (additionalData.ContainsKey("HttpMethod"))
                        transaction.HttpMethod = additionalData["HttpMethod"]?.ToString();
                    if (additionalData.ContainsKey("Endpoint"))
                        transaction.Endpoint = additionalData["Endpoint"]?.ToString();
                    if (additionalData.ContainsKey("HttpStatusCode"))
                        transaction.HttpStatusCode = Convert.ToInt32(additionalData["HttpStatusCode"]);
                    if (additionalData.ContainsKey("RetryCount"))
                        transaction.RetryCount = Convert.ToInt32(additionalData["RetryCount"]);
                }

                return await LogTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryService] Error logging failed transaction");
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "LogFailedTransactionAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage);
            }
        }

    }
}
