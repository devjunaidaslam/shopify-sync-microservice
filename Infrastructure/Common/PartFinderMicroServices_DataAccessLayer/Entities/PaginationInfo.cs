using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities
{
    public class PaginationInfo
    {
        public int PageNo { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItemCount { get; set; }
        public int NextPage { get; set; }
        public int PrevPage { get; set; } 
    }

}
