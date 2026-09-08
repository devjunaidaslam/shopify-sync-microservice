using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.Authentication.Login;
using ShopifySync_DataAccessLayer.Entities.Authentication.Register;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IAccountService
    {
        Task<bool> IsUserExist(RegisterUserModel registerUser);
        //  Task<Response> CreateAsyncUserWithToken(RegisterUser registerEngineer);
        Task<Response> LoginAsync(LoginModel loginModel);

		Task<Response> RefreshToken(RefreshTokenModel model);
        Task<Response> ForgotPassword(ForgotPasswordModel forgotPasswordModel);
        Task<Response> ChangeUserPasswordFromMail(ChangePasswordModelFromMail model); 
        Task<Response> ChangePassword(ChangePasswordModel model);
        Task<Response> GetUserInfo();
        Task<Response> UpdateUserInfo(CreateUserModel model);
        Task<Response> Logout();
    }
}
