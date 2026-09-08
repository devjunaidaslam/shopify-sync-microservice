using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.Supplier
{
    public class SupplierCreateDTO
    {
        public string Name { get; set; }

        public bool? IsDefault { get; set; }

        public string? SupplierNumber { get; set; }

        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        public string? SupplierPredikoId { get; set; }
    }
}
