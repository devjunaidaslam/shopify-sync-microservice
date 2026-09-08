using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.ModelDTO
{
    public class ModelDTO
    {
        public long VehicleModelId { get; set; }

        public string Name { get; set; }

        public long? ReferenceId { get; set; }

        public long? SupplierId { get; set; }

        public string LocalName { get; set; }

        public bool IsActive { get; set; }
    }
}
