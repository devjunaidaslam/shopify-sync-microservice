using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class AspNetUser : IdentityUser
    {
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? FirstName { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? LastName { get; set; }

        public string? UserRoleId { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? EmailVerificationOTP { get; set; }

        public DateTime? EmailVerificationOTPExpiration { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? UserProfileImage { get; set; }
    }
}
