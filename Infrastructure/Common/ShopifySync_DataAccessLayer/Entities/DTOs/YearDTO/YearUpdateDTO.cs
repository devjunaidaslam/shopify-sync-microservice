using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.YearDTO
{
    public class YearUpdateDTO
    {
        public long VehicleYearId { get; set; }

        public string Name { get; set; }

        public long ReferenceId { get; set; }

        public long SupplierId { get; set; }
    }
}
