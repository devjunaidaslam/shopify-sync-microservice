using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class Suppliers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SupplierId { get; set; }

        public string Name { get; set; }

        [Column("is_default")]
        public bool? IsDefault { get; set; }

        [Column("supplier_number")]
        public string? SupplierNumber { get; set; }

        [Column("code")]
        public string? Code { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("supplier_prediko_id")]
        public string? SupplierPredikoId { get; set; }
    }
}
