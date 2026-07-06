using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    /// <summary>
    /// Entity for tracking Shopify transaction history across all microservices
    /// </summary>
    [Table("ShopifyTransactionHistory")]
    public class ShopifyTransactionHistory
    {
        [Key]
        public int Id { get; set; }

        // Product-related identifiers
        [MaxLength(100)]
        public string? ProductId { get; set; }                    // Shopify Product ID

        // Transaction Details
        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; }               // "send" or "receive"

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; }                     // "product", "variant", "inventory", "fitment", "collection", "price", "webhook"

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }                        // "success", "failed", "pending", "retry"

        // Payload and Response
        [Column(TypeName = "text")]
        public string? Payload { get; set; }                      // JSON payload sent/received

        [Column(TypeName = "text")]
        public string? ErrorMessage { get; set; }                 // Error details if failed

        // Timestamps
        public DateTime SentAt { get; set; }                      // When transaction was initiated

        public DateTime? ReceivedBy { get; set; }                 // When received (for webhooks)

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}