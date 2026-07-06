using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class VehicleModelDetails
    {
        [Key]
        public long VehicleModelId { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string OEMNumber { get; set; }

        [Column(TypeName = "Integer")]
        public long Year { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string Make { get; set; }

        [Column(TypeName = "nvarchar(50)")]
       
        public string Model { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Category { get; set; }

        [Column(TypeName = "Integer")]
        public long Supplier { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
