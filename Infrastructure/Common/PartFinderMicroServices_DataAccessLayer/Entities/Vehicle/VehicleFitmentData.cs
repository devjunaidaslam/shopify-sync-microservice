using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.Vehicle
{
    public class VehicleFitmentData
    {
        public string Type { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }
}