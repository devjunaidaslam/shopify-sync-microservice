using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest
{
    public class UpdateVariantLocationPriceRequest
    {
        [Required]
        public long VariantId { get; set; }
        public List<LocationPricePair> LocationPricePairs { get; set; }
    }

    public class LocationPricePair
    {
        public string LocationId { get; set; }
        public decimal NewPrice { get; set; }
    }
}
