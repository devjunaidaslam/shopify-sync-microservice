using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Model
{
    public class VehicleTypes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VehicleTypesId { get; set; }

        public string Name { get; set; }


        public string Name_en { get; set; }

        public long? ReferenceId { get; set; }

        [ForeignKey("Supplier")]
        public long SupplierId { get; set; }

        public string LocalName { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
