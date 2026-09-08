using System;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO
{
    public class HistoryInventoryStatusDto
    {
        public int Id { get; set; }
        public int TotalProductsProcessed { get; set; }
        public int TotalCursors { get; set; }
        public int TotalCursorsProcessed { get; set; }
        public int TotalCursorsFailed { get; set; }
        public bool InProgress { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public DateTime? LastRecordDate { get; set; }
        public DateTime? ProcessStartDate { get; set; }
        public DateTime? ProcessEndDate { get; set; }
        public string? ErrorMessage { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public int? ProcessingProgress { get; set; } // Percentage
    }
}
