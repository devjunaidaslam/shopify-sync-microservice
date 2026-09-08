using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.Supplier
{
    public class SupplierUpdateDTO
    {
        public long SupplierId { get; set; }

        public string Name { get; set; }

        public bool? IsDefault { get; set; }

        public string? SupplierNumber { get; set; }

        public string? Code { get; set; }

        public bool IsActive { get; set; }

        public string? SupplierPredikoId { get; set; }
    }
}
