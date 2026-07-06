using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class OEMFilterModel
    {
        public string? search { get; set; } = "";
        public int? page_no { get; set; } = 1;
        public string? per_page { get; set; } = null;
    }
}
