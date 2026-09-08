using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.Authentication.Register
{
    public class ChangePasswordModelFromMail
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; }

		[Required(ErrorMessage = "New password is required")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirmed password is required")]
        public string ConfirmedPassword { get; set; }
    }

	public class ChangePasswordModel
	{

		[Required(ErrorMessage = "Current password is required")]
		public string CurrentPassword { get; set; }

		[Required(ErrorMessage = "New password is required")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "Confirmed password is required")]
		public string ConfirmedPassword { get; set; }
	}
}
