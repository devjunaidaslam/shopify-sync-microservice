namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class Product
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set; }
        public string? DescriptionHtml { get; set; }
        public string? DescriptionHtml_en { get; set; }
        public int VendorId { get; set; }
        public int? ProductTypeId { get; set; }

       // public string? Vendor { get; set; }

        public string? Handle { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CompareAtPrice { get; set; }
        public decimal CompareAtPriceMax { get; set; }
        public decimal CompareAtPriceMin { get; set; }
        public bool CompareAtPriceVaries { get; set; }
        public bool Is_Piece { get; set; }
        public bool Exact_Fit {  get; set; }
        public DateTime ? LocalCreatedAt { get; set; } 
        public DateTime ? LocalUpdatedAt { get; set; } 
        public List<ProductTag> ProductTags { get; set; }
        public List<ProductCollection> ProductCollections { get; set; }
        public List<Variant> Variants { get; set; }
        public Vendor? Vendor { get; set; }
        public ProductType? ProductType { get; set; }
        public List<ProductOption> ProductOptions { get; set; }
        public List<ProductImage> ProductImages { get; set; }

    }
}
