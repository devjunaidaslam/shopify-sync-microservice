using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Enum
{
    public enum UserRole
    {
        [Display(Name = "System Admin")]
        SystemAdmin = 1,

        [Display(Name = "Supplier")]
        Supplier = 2,

        [Display(Name = "Technician")]
        Technician = 3,

        [Display(Name = "User")]
        User = 4,
    }
}
