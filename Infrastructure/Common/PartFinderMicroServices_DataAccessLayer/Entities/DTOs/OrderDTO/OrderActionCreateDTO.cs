using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO
{
    public class OrderActionCreateDTO
    {
        [Required]
        public long OrderId { get; set; }
        
        public long? FulfillmentOrderId { get; set; }
        
        [Required]
        public string ActionType { get; set; }  // "SUPPLIER_PRODUCT_ORDER" | "TRANSFER"
        
        [Required]
        public string Reason { get; set; }
        
        public long? FromLocationId { get; set; }
        public long? ToLocationId { get; set; }
        
        [Required]
        public string Payload { get; set; }
        
        [Required]
        public string IdempotencyKey { get; set; }
    }
}
