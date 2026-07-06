using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.MakeDTO
{
    public class MakeCreateDTO
    {
        public string Name { get; set; }

        public long? ReferenceId { get; set; }

        public long? SupplierId { get; set; }

        public string LocalName { get; set; }
    }
}
