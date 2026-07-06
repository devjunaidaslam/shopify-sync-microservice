using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class TempVehicleFilterModel
    {
        public long? ImportFitmentId { get; set; }

        public int? page_no { get; set; } = 1;

        public string? per_page { get; set; }

        public bool? HasConflicts { get; set; }
    }
}
