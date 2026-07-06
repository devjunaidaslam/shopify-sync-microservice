using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    /// <summary>
    /// Service interface for Shopify transaction history operations
    /// </summary>
    public interface ITransactionHistoryService
    {
        /// <summary>
        /// Logs a new transaction to the history
        /// </summary>
        /// <param name="transaction">Transaction details to log</param>
        /// <returns>Response indicating success or failure</returns>
        Task<Response> LogTransactionAsync(CreateTransactionDto transaction);

        /// <summary>
        /// Gets transaction history records with search and pagination.
        /// Search filters by ProductId, TransactionType, EventType, and Status using case-insensitive matching.
        /// </summary>
        /// <param name="filter">Filter and pagination options</param>
        /// <returns>Response containing transaction history records</returns>
        Task<Response> GetTransactionsAsync(TransactionFilterDto filter);

        /// <summary>
        /// Cleans up old transaction history records based on retention policy
        /// </summary>
        /// <param name="retentionDays">Number of days to retain records</param>
        /// <returns>Response indicating number of records cleaned up</returns>
        Task<Response> CleanupOldRecordsAsync(int retentionDays);

        /// <summary>
        /// Logs a successful transaction
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="eventType">Event type</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="payload">Transaction payload</param>
        /// <param name="response">Transaction response</param>
        /// <param name="additionalData">Additional transaction data</param>
        /// <returns>Response indicating success or failure</returns>
        Task<Response> LogSuccessTransactionAsync(string? productId, string eventType, string microservice, 
            string? payload = null, string? response = null, Dictionary<string, object>? additionalData = null);

        /// <summary>
        /// Logs a failed transaction
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="eventType">Event type</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="payload">Transaction payload</param>
        /// <param name="additionalData">Additional transaction data</param>
        /// <returns>Response indicating success or failure</returns>
        Task<Response> LogFailedTransactionAsync(string? productId, string eventType, string microservice, 
            string errorMessage, string? payload = null, Dictionary<string, object>? additionalData = null);
    }
}
