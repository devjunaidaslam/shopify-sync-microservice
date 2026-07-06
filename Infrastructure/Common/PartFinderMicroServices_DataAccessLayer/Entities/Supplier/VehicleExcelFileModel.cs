using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.Supplier
{
    public class VehicleExcelFileModel
    {
        public long SupplierId { get; set; }

        public IFormFile File { get; set; }

        public Boolean Isdefault { get; set; }
    }
}
