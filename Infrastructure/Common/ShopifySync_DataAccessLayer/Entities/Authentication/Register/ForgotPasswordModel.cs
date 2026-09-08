using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.Authentication.Register
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
    }
}
