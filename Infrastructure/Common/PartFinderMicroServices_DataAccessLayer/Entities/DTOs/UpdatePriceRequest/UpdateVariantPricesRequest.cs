using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdatePriceRequest
{
    public class UpdateVariantPricesRequest
    {
        [Required]
        public long VariantId { get; set; } 
        public string NewPrice { get; set; }   
        public string NewCompareAtPrice { get; set; }
    }


}
