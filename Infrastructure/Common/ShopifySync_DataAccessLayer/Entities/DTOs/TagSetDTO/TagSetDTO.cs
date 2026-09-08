using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShopifySync_DataAccessLayer.Model;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.TagSetDTO
{
   public class TagSetDTO
    {
        public List<Tag> Tags { get; set; }
        public List<ProductTag> ProductTags { get; set; }
    }
}
