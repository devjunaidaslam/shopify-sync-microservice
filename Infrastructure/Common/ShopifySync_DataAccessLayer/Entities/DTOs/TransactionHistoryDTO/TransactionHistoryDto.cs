using System.ComponentModel.DataAnnotations;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO
{
    

    /// <summary>
    /// DTO for creating new transaction history records
    /// </summary>
    public class CreateTransactionDto
    {
        // Product-related identifiers
        public string? ProductId { get; set; }
        public string? VariantId { get; set; }
        public string? CollectionId { get; set; }
        public string? InventoryItemId { get; set; }
        public string? LocationId { get; set; }

        // Transaction Details
        [Required]
        public string TransactionType { get; set; } = string.Empty; // "send" or "receive"

        [Required]
        public string EventType { get; set; } = string.Empty; // "product", "variant", "inventory", "fitment", "collection", "price", "webhook"

        [Required]
        public string Status { get; set; } = string.Empty; // "success", "failed", "pending", "retry"

        [Required]
        public string Microservice { get; set; } = string.Empty; // "ShopifyService", "ShopifyConnector", "PartService", "AuthService"

        // Payload and Response
        public string? Payload { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }

        // Timestamps
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReceivedBy { get; set; }
        public DateTime? SentToShopifyAt { get; set; }
        public DateTime? ResponseReceivedAt { get; set; }

        // Additional Context
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }
        public string? UserId { get; set; }
        public string? WebhookId { get; set; }
        public int? RetryCount { get; set; }
        public string? HttpMethod { get; set; }
        public string? Endpoint { get; set; }
        public int? HttpStatusCode { get; set; }
    }

    /// <summary>
    /// DTO for filtering and pagination of transaction history
    /// </summary>
    public class TransactionFilterDto
    {
        public string search { get; set; } = string.Empty;
        public int page_no { get; set; } = 1;
        public int page_size { get; set; } = 50;
    }

}
