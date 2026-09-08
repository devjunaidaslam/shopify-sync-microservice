using System.ComponentModel.DataAnnotations;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.OEMVehicleDTO
{
    public class OemVehicleLinkDTO
    {
        [Required]
        public int OEMId { get; set; }

        [Required]
        public long VehicleId { get; set; }

        public long? SupplierId { get; set; }
    }
}
