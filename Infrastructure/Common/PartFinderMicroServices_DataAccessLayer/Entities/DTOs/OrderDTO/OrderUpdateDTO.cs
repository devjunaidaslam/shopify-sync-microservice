using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO
{
    public class OrderUpdateDTO
    {
        [Required]
        public long Id { get; set; }
        
        public string SourceName { get; set; }
        public long? SaleLocationId { get; set; }
        public string SaleLocationName { get; set; }
        public string Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RawPayload { get; set; }
    }
}
