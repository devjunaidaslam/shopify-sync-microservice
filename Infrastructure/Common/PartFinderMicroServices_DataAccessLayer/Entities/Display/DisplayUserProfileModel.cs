using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.Display
{

    public class DisplayUserProfileModel
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string ProfileImageUrl { get; set; }

        public string UserRole { get; set; }

        public bool IsActive { get; set; }
    }

    public class UserProfileListModel
    {
        public string search { get; set; }

        public int Page_No { get; set; }

        public string Per_Page { get; set; } // ← keep string to allow "all"

    }
}
