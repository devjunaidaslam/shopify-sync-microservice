using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class OemVehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OEMVehicleId { get; set; }
        public string OEM { get; set; }
        public int OEMId { get; set; }
        public long? VehicleId { get; set; }
        public long? SupplierId { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}