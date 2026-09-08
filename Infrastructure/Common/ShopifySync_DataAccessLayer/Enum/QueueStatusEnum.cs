using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Enum
{
    public enum QueueStatus
    {
        [Display(Name = "Failed")]
        Failed = 1,

        [Display(Name = "Completed")]
        Completed = 2
    }
}