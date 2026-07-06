using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class Vehicles
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VehicleId { get; set; }

        public long? TypeId { get; set; }

        public long? YearId { get; set; }

        public long? MakeId { get; set; }

        public long? ModelId { get; set; }

        public DateTime? LastUpdate { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }
    }
}
