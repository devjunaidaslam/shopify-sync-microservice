using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest
{
    /// <summary>
    /// Request DTO for updating inventory levels in bulk
    /// </summary>
    public class UpdateInventoryRequest
    {
        /// <summary>
        /// Array of inventory items to update
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one inventory item is required")]
        public List<UpdateInventoryItem> InventoryItems { get; set; } = new List<UpdateInventoryItem>();
    }

    /// <summary>
    /// Individual inventory item to update
    /// </summary>
    public class UpdateInventoryItem
    {
        /// <summary>
        /// The Shopify location ID where inventory should be updated
        /// </summary>
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "LocationId must be a positive number")]
        public long LocationId { get; set; }

        /// <summary>
        /// The new available quantity for the variant at this location
        /// </summary>
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "AvailableQuantity must be zero or positive")]
        public int AvailableQuantity { get; set; }

        /// <summary>
        /// The Shopify variant ID to update
        /// </summary>
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "VariantId must be a positive number")]
        public long VariantId { get; set; }
    }
}
