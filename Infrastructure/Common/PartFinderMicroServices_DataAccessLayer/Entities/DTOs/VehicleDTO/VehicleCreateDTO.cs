using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO
{
    public class VehicleCreateDTO
    {
        public long TypeId { get; set; }

        public long YearId { get; set; }

        public long MakeId { get; set; }

        public long ModelId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
