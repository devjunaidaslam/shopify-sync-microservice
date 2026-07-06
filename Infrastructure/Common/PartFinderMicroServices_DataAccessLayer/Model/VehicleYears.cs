using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class VehicleYears
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VehicleYearId { get; set; }

        public string Name { get; set; }

        public long? ReferenceId { get; set; }

        public long? SupplierId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
