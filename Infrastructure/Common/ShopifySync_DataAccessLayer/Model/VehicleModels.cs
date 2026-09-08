using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Model
{
    public class VehicleModels
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VehicleModelId { get; set; }

        public string Name { get; set; }

        public long? ReferenceId { get; set; }

        public long? SupplierId { get; set; }

        [Column("Local_name")]
        public string? LocalName { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
