namespace ShopifySync_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO
{
    public class HistoryInventoryFilterDto
    {
        public string search { get; set; } = string.Empty;
        public int page_no { get; set; } = 1;
        public int page_size { get; set; } = 20;
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public bool? inProgress { get; set; }
        public bool? isSuccess { get; set; }
    }
}
