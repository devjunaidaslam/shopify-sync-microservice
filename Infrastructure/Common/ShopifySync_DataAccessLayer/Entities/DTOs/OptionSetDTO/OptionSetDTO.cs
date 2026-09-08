using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.OptionSetDTO
{
   public class OptionSetDTO
    {
        public List<Option> Options { get; set; }
        public List<ProductOption> ProductOptions { get; set; }
        public List<OptionValue> OptionValues { get; set; }
    }
       
}
