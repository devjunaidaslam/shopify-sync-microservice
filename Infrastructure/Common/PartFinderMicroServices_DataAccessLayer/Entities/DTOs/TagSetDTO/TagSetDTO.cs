using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PartFinderMicroServices_DataAccessLayer.Model;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TagSetDTO
{
   public class TagSetDTO
    {
        public List<Tag> Tags { get; set; }
        public List<ProductTag> ProductTags { get; set; }
    }
}
