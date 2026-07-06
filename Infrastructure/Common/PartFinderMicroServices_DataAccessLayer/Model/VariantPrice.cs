namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class VariantPrice
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int LocationId { get; set; }
        public string? Price { get; set; }
        public int? CompareAtPrice { get; set; }
        public int Currency { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Variant Variant { get; set; }
        public Location Location { get; set; }
    }
}
