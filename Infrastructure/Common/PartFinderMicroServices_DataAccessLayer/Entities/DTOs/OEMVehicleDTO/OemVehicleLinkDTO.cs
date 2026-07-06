using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OEMVehicleDTO
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
