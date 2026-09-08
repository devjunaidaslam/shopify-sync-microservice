using Microsoft.Extensions.Logging;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Helper class for easy integration of transaction history logging across microservices
    /// </summary>
    public class TransactionHistoryHelper
    {
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly ILogger<TransactionHistoryHelper> _logger;

        public TransactionHistoryHelper(ITransactionHistoryService transactionHistoryService, ILogger<TransactionHistoryHelper> logger)
        {
            _transactionHistoryService = transactionHistoryService;
            _logger = logger;
        }

        /// <summary>
        /// Logs a successful Shopify API call
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="eventType">Event type (product, variant, inventory, fitment, etc.)</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="payload">Request payload</param>
        /// <param name="response">Response data</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="httpStatusCode">HTTP status code</param>
        /// <param name="additionalData">Additional context data</param>
        public async Task LogSuccessAsync(string? productId, string eventType, string microservice, 
            string? payload = null, string? response = null, string? httpMethod = null, 
            string? endpoint = null, int? httpStatusCode = null, Dictionary<string, object>? additionalData = null)
        {
            try
            {
                var data = additionalData ?? new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(httpMethod)) data["HttpMethod"] = httpMethod;
                if (!string.IsNullOrEmpty(endpoint)) data["Endpoint"] = endpoint;
                if (httpStatusCode.HasValue) data["HttpStatusCode"] = httpStatusCode.Value;

                await _transactionHistoryService.LogSuccessTransactionAsync(productId, eventType, microservice, payload, response, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryHelper] Error logging success transaction");
            }
        }

        /// <summary>
        /// Logs a failed Shopify API call
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="eventType">Event type</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="payload">Request payload</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="httpStatusCode">HTTP status code</param>
        /// <param name="additionalData">Additional context data</param>
        public async Task LogFailureAsync(string? productId, string eventType, string microservice, 
            string errorMessage, string? payload = null, string? httpMethod = null, 
            string? endpoint = null, int? httpStatusCode = null, Dictionary<string, object>? additionalData = null)
        {
            try
            {
                var data = additionalData ?? new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(httpMethod)) data["HttpMethod"] = httpMethod;
                if (!string.IsNullOrEmpty(endpoint)) data["Endpoint"] = endpoint;
                if (httpStatusCode.HasValue) data["HttpStatusCode"] = httpStatusCode.Value;

                await _transactionHistoryService.LogFailedTransactionAsync(productId, eventType, microservice, errorMessage, payload, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryHelper] Error logging failed transaction");
            }
        }

        /// <summary>
        /// Logs a webhook received event
        /// </summary>
        /// <param name="productId">Product ID from webhook</param>
        /// <param name="eventType">Webhook event type</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="payload">Webhook payload</param>
        /// <param name="webhookId">Shopify webhook ID</param>
        /// <param name="additionalData">Additional context data</param>
        public async Task LogWebhookReceivedAsync(string? productId, string eventType, string microservice, 
            string? payload = null, string? webhookId = null, Dictionary<string, object>? additionalData = null)
        {
            try
            {
                var data = additionalData ?? new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(webhookId)) data["WebhookId"] = webhookId;

                var transaction = new ShopifySync_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO.CreateTransactionDto
                {
                    ProductId = productId,
                    TransactionType = "receive",
                    EventType = eventType,
                    Status = "success",
                    Microservice = microservice,
                    Payload = payload,
                    ReceivedBy = DateTime.UtcNow,
                    WebhookId = webhookId
                };

                await _transactionHistoryService.LogTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryHelper] Error logging webhook received");
            }
        }

        /// <summary>
        /// Logs a fitment data update
        /// </summary>
        /// <param name="variantId">Variant ID</param>
        /// <param name="productId">Product ID</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="fitmentData">Fitment data sent to Shopify</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="errorMessage">Error message if failed</param>
        public async Task LogFitmentUpdateAsync(string variantId, string? productId, string microservice, 
            string? fitmentData = null, bool success = true, string? errorMessage = null)
        {
            try
            {
                var additionalData = new Dictionary<string, object>
                {
                    ["VariantId"] = variantId
                };

                if (success)
                {
                    await LogSuccessAsync(productId, "fitment", microservice, fitmentData, null, "POST", "/admin/api/2023-04/variants/{id}/metafields.json", 200, additionalData);
                }
                else
                {
                    await LogFailureAsync(productId, "fitment", microservice, errorMessage ?? "Fitment update failed", fitmentData, "POST", "/admin/api/2023-04/variants/{id}/metafields.json", 400, additionalData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryHelper] Error logging fitment update");
            }
        }

        /// <summary>
        /// Logs an inventory update
        /// </summary>
        /// <param name="inventoryItemId">Inventory item ID</param>
        /// <param name="locationId">Location ID</param>
        /// <param name="microservice">Microservice name</param>
        /// <param name="inventoryData">Inventory data</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="errorMessage">Error message if failed</param>
        public async Task LogInventoryUpdateAsync(string inventoryItemId, string locationId, string microservice, 
            string? inventoryData = null, bool success = true, string? errorMessage = null)
        {
            try
            {
                var additionalData = new Dictionary<string, object>
                {
                    ["InventoryItemId"] = inventoryItemId,
                    ["LocationId"] = locationId
                };

                if (success)
                {
                    await LogSuccessAsync(null, "inventory", microservice, inventoryData, null, "POST", "/admin/api/2023-04/inventory_levels/adjust.json", 200, additionalData);
                }
                else
                {
                    await LogFailureAsync(null, "inventory", microservice, errorMessage ?? "Inventory update failed", inventoryData, "POST", "/admin/api/2023-04/inventory_levels/adjust.json", 400, additionalData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryHelper] Error logging inventory update");
            }
        }
    }
}
