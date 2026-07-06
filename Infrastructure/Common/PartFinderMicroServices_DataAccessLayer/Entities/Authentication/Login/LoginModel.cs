using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.Authentication.Login
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        [MaxLength(50)]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(50)]
        public string Password { get; set; }
    }
}
