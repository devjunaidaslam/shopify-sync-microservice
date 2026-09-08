namespace ShopifySync_DataAccessLayer.Model
{
    public class ProductType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime LocalCreatedAt { get; set; }
        public DateTime LocalUpdatedAt { get; set; }

        // Navigation property for products with this type
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
