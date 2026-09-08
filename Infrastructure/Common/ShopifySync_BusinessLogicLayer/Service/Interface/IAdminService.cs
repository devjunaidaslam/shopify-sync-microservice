using ShopifySync_DataAccessLayer.Entities.Authentication.Register;
using ShopifySync_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.Display;
using Microsoft.AspNetCore.Http;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IAdminService
    {
        Task<Response> AddNewUser(RegisterUser registerUser);

        Task<Response> UploadUserPhotoToS3(IFormFile file);

		Task<Response> GetUsersList(string search, int pageNo, string pageSize);
        Task<Response> GetUserInfoByUserEmail(string email);
        Task<Response> UpdateUserInfoByUserEmail(CreateUserModel model);
        Task<Response> ActiveDeactiveUser(UserEmail model);
    }
}
