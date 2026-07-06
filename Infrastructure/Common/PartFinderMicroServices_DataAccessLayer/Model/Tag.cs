namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class Tag
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<ProductTag> ProductTags { get; set; }
    }
}
