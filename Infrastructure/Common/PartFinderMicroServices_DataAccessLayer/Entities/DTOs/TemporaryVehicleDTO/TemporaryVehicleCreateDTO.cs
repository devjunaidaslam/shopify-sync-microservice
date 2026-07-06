using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO
{
    public class TemporaryVehicleCreateDTO
    {
        public long? ImportFitmentId { get; set; } // Nullable in case of optional FK

        public string type { get; set; }


        public int year { get; set; }

        public string make { get; set; }

        public string model { get; set; }

        public string? oem { get; set; }


        public long? type_id { get; set; }

        public long? year_id { get; set; }

        public long? make_id { get; set; }

        public long? model_id { get; set; }

        public bool? is_default_type { get; set; }

        public bool? is_default_year { get; set; }

        public bool? is_default_make { get; set; }

        public bool? is_default_model { get; set; }

        public bool? is_processed { get; set; }

        public DateTime created_at { get; set; }

        public DateTime? updated_at { get; set; }

    }
}
