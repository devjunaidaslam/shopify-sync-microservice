using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ShopifySync_DataAccessLayer.Entities.Authentication.Register
{
    public class RegisterUserModel
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        //[Required(ErrorMessage = "Password is required")]
        // public string Password { get; set; }

        [RegularExpression(@"^\+1[ -]?(\d{3})[ -]?(\d{3})[ -]?(\d{4})$", ErrorMessage = "Invalid phone number format.")]
        public string? ContactNumber { get; set; }


        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

		public IFormFile? ProfileImage { get; set; }

		//[Required(ErrorMessage = "Profile Image is required")]
		//public IFormFile? ProfileImage { get; set; }

	}

    public class RegisterUser : RegisterUserModel
    {
        public string? Role { get; set; }

    }

    public class CreateUserModel
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [RegularExpression(@"^\+1[ -]?(\d{3})[ -]?(\d{3})[ -]?(\d{4})$", ErrorMessage = "Invalid phone number format.")]
        public string? ContactNumber { get; set; }

		//[Required(ErrorMessage = "Email is required")]
		public IFormFile? ProfileImage { get; set; }

        public bool RemoveProfileImage { get; set; } = false;

	}

    public class UserEmail
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "IsUserActive is required")]
        public bool IsUserActive { get; set; }

        
    }

    public class CreatedUserResponseModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? ContactNumber { get; set; }

        public string Role { get; set; } 
        public string UserProfileImage { get; set; }

       
    }


}
