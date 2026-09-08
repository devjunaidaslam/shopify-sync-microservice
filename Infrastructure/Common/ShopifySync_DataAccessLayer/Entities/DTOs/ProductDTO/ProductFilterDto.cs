namespace ShopifySync_DataAccessLayer.Entities.DTOs.ProductDTO
{
    public class ProductFilterDto
    {
        public string search { get; set; } = string.Empty;
        public int page_no { get; set; } = 1;
        public int page_size { get; set; } = 20;
    }
}
