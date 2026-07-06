using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO
{
    public class VehicleDTO
    {
        public long VehicleId { get; set; }

        public string TypeName { get; set; }

        public string YearName { get; set; }

        public string MakeName { get; set; }

        public string ModelName { get; set; }

        public bool? IsActive { get; set; }
    }
}
