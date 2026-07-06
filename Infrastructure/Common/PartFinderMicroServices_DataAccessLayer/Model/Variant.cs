namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class Variant
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public int ProductId { get; set; }
        public string? Title { get; set; }
        public string? SKU { get; set; }
        public string? Price { get; set; }
        public string? CompareAtPrice { get; set; }
        public string? Barcode { get; set; }
        public float Weight { get; set; } = 0.0f;
        public string? WeightUnit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? OEMMetaField { get; set; }
        public int? OEMId { get; set; }

        public Product Product { get; set; }
        public OEM OEM { get; set; }
        public List<VariantOptionValue> VariantOptionValues { get; set; }
        public List<InventoryLevel> InventoryLevels { get; set; }
        public List<VariantPrice> VariantPrices { get; set; }
        public List<OemVariant> OemVariants { get; set; }
    }
}
