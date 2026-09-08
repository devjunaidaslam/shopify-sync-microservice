namespace ShopifySync_DataAccessLayer.Model
{
    public class Collection
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set;}
        public string? Description { get; set; }
        public string? Image { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<ProductCollection> ProductCollections { get; set; }
    }

}
