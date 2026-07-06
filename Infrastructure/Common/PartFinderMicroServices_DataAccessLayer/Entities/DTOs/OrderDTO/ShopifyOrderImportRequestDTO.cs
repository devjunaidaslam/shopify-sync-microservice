using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO
{
    /// <summary>
    /// Request DTO for importing an order from Shopify
    /// </summary>
    public class ShopifyOrderImportRequestDTO
    {
        /// <summary>
        /// Numeric Shopify Order ID (e.g., 5493840248972)
        /// </summary>
        [Required]
        public long ShopifyOrderId { get; set; }

        /// <summary>
        /// Optional: Set of supplier/fournisseur location names (case-insensitive) for Drop Ship logic
        /// If empty, defaults to {"FOURNISSEUR"}
        /// </summary>
        public HashSet<string>? SupplierLocationNames { get; set; }
    }
}
