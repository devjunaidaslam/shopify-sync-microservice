using System.ComponentModel.DataAnnotations;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO
{
    public class BulkUpdateVehicleAttributeDTO
    {
        [Required]
        public long TempVehicleImportId { get; set; }

        [Required]
        public string AttributeType { get; set; } // "type", "make", "year", "model"

        [Required]
        public string AttributeValue { get; set; } // The current value to match (e.g., "YZ 450 F")

        public long? NewAttributeId { get; set; } // The new ID to set (optional)

        public bool SetAsDefault { get; set; } // Whether to set is_default_X to true
    }
}
