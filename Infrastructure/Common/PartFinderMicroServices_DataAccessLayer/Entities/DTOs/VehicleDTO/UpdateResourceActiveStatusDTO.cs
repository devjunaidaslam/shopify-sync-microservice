using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO
{
    public class UpdateResourceActiveStatusDTO
    {
        [Required]
        public long ResourceId { get; set; }

        [Required]
        public string ResourceType { get; set; } // "type", "year", "make", "model"

        [Required]
        public bool IsActive { get; set; }
    }
}
