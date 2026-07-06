namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class InventoryLevel
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int LocationId { get; set; }
        public int Available { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Variant Variant { get; set; }
        public Location Location { get; set; }
    }

}
