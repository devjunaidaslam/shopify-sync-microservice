using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.Supplier
{
    public class PartDetail
    {
       public string OEMNumber { get; set; }
       public long Year { get; set; }
       public string Make { get; set; }
       public string Model { get; set; }
       public string Category { get; set; }
       public long Supplier { get; set; }
    }
}
