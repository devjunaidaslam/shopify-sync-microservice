namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs
{
    /// <summary>
    /// DTO for fitment upsert request
    /// </summary>
    public class FitmentUpsertDTO
    {
        /// <summary>
        /// The mode for processing variants: "by_variant", "by_product", or "all_variants"
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// The variant ID (required when mode is "by_variant")
        /// </summary>
        public string? VariantId { get; set; }

        /// <summary>
        /// The product ID (required when mode is "by_product")
        /// </summary>
        public string? ProductId { get; set; }

        public bool Filter { get; set; } = true;
    }
} 