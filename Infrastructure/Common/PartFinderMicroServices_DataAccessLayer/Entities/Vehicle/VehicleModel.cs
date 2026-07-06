using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.Vehicle
{
    public class VehicleModel
    {
        public class VehicleFilterModel
        {
            public long TypeId { get; set; }
            public long YearId { get; set; }
            public long MakeId { get; set; }

            public long ModelId { get; set; }
            public int? OemId { get; set; }

            public string? search { get; set; } = "";

            public int? page_no { get; set; } = 1;

            public string per_page { get; set; } = "all";
        }
    }
}
