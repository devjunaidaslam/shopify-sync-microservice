namespace ShopifySync_DataAccessLayer.Model
{
    public class VariantOptionValue
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int OptionValueId { get; set; }
        public string? Position { get; set; }
        public Variant Variant { get; set; }
        public OptionValue OptionValue { get; set; }
    }
}
