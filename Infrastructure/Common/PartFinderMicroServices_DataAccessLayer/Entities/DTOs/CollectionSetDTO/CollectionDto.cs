using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.CollectionSetDTO
{
    public class CollectionDto
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}