using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.MakeDTO
{
    public class MakeDTO
    {
        public long VehicleMakeId { get; set; }

        public string Name { get; set; }

        public long? ReferenceId { get; set; }

        public long? SupplierId { get; set; }

        public string LocalName { get; set; }

        public bool IsActive { get; set; }
    }
}
