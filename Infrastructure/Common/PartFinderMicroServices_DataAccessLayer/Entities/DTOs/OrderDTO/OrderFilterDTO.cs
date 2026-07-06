namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO
{
    public class OrderFilterDTO
    {
        public string? search { get; set; }
        public int page_no { get; set; } = 1;
        public int page_size { get; set; } = 10;
        public long? CustomerId { get; set; }
        public string? SourceName { get; set; }  // "pos" or "web"
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
