using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_DataAccessLayer.Entities.Authentication.Login;
using PartFinderMicroServices_DataAccessLayer.Entities.Authentication.Register;
using PartFinderMicroServices_DataAccessLayer.Entities.Email;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Enum;
using PartFinderMicroServices_DataAccessLayer.Model;
using System.IdentityModel.Tokens.Jwt;
using PartFinderMicroServices_BusinessLogicLayer.Functions;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_DataAccessLayer.Entities.Display;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    public class AccountService : IAccountService
    {

        #region Fields
        private readonly UserManager<AspNetUser> _userManager;
        private readonly SignInManager<AspNetUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IEmailService _emailService;
		private readonly IS3StorageService _storageService;
		private readonly HashSet<string> _revokedTokens = new();
		private readonly object _lock = new();

		private readonly PartFinderDbContext _context;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public AccountService(
            UserManager<AspNetUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<AspNetUser> signInManager,
            IConfiguration configuration,
            ICommonService commonService,
            IEmailService emailService,
			IS3StorageService storageService,
			PartFinderDbContext context
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _commonService = commonService;
            _emailService = emailService;
			_storageService = storageService;
			_context = context;
        }
        #endregion

        #region Register

        public async Task<bool> IsUserExist(RegisterUserModel registerUser)
        {
            var user = await _userManager.FindByEmailAsync(registerUser.Email);
            if (user == null)
            {
                return false;
            }
            return true;
        }

        //public async Task<Response> CreateAsyncUserWithToken(RegisterUser registerUser)
        //{
        //    Response response = new Response();
        //    try
        //    {

        //        var userExists = await _userManager.FindByEmailAsync(registerUser.Email);
        //        if (userExists != null)
        //        {

        //            response.IsSuccess = false;
        //            response.Message = _ApiResponseMessageList.AccountAlreadyExist;
        //            response.StatusCode = 409;
        //            return response;
        //        }
        //        else
        //        {
        //            var userRole = _roleManager.Roles.FirstOrDefault(r => r.Name == EnumExtensions.GetEnumDisplayName(UserRole.User));
        //            if (userRole == null)
        //            {
        //                await _roleManager.CreateAsync(new IdentityRole("User"));
        //                userRole = _roleManager.Roles.FirstOrDefault(r => r.Name == EnumExtensions.GetEnumDisplayName(UserRole.User));
        //            }

        //            AspNetUser user = new()
        //            {
        //                Email = registerUser.Email,
        //                SecurityStamp = new Guid().ToString(),
        //                UserName = registerUser.Email,
        //                FirstName = registerUser.FirstName,
        //                LastName = registerUser.LastName,
        //                PhoneNumber = registerUser.ContactNumber,
        //                CreatedDate = DateTime.UtcNow,
        //                UserRoleId = userRole.Id,
        //                UserProfileImage = registerUser.ProfileImageName
        //            };


        //            if (await _roleManager.RoleExistsAsync(EnumExtensions.GetEnumDisplayName(UserRole.User)))
        //            {
        //                var result = await _userManager.CreateAsync(user, registerUser.Password);

        //                if (!result.Succeeded)
        //                {
        //                    var errorMsg = _ApiResponseMessageList.UserFailedToRegister;
        //                    response.IsSuccess = false;
        //                    response.Message = errorMsg;
        //                    response.StatusCode = 400;
        //                    return response;
        //                }

        //                var otp = CommonFunction.GenerateSixDigitOTP();
        //                DateTime expirationTime = DateTime.UtcNow.AddMinutes(10);

        //                var authClaim = new List<Claim>
        //                    {
        //                        new Claim("UserId", user.Id),
        //                        new Claim("EmailId", registerUser.Email),
        //                        new Claim("UserType",EnumExtensions.GetEnumDisplayName(UserRole.User)),
        //                        new Claim("ExpireDate", DateTime.UtcNow.ToString()),

        //                        new Claim(ClaimTypes.Name, user.UserName),
        //                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //                    };


        //                var jwtToken = CommonFunction.GetAccessToken(authClaim);

        //                int RoleId = 3;
        //                var UserRoleName = EnumExtensions.GetEnumDisplayName(UserRole.User);

        //                if (Enum.TryParse(typeof(UserRole), UserRoleName, out object? enumValue))
        //                {
        //                    RoleId = (int)enumValue;
        //                }


        //                user.EmailVerificationOTP = otp;
        //                user.EmailVerificationOTPExpiration = expirationTime;
        //                await _userManager.UpdateAsync(user);

        //                var sendOTP = otp.ToString(); ;

        //                response.IsSuccess = true;
        //                response.Message = _ApiResponseMessageList.UserRegisteredSuccessMessage;
        //                response.StatusCode = 200;
        //                response.Data = new
        //                {
        //                    UserId = user.Id,
        //                    UserTypeName = UserRoleName,
        //                    UserTypeId = RoleId,
        //                    Token = jwtToken
        //                };

        //                //string body = string.Empty;
        //                //string HTMLPath = Path.GetFullPath("Views/EmailVerification.html").Replace("~\\", "");
        //                //using (StreamReader reader = new StreamReader(HTMLPath))
        //                //{
        //                //    body = reader.ReadToEnd();
        //                //}

        //                //body = body.Replace("@MemberName", registerUser.FirstName)
        //                //        .Replace("@OTPNumber", sendOTP);

        //                //var environment = Settings.GetEnvironment();
        //                //var subject = $"[Part Finder {environment}] Email Verification Required";

        //                //if (registerUser.Email is null)
        //                //{
        //                //    throw new InvalidOperationException("Engineer email is null.");
        //                //}

        //              //  var message = new Message(new string[] { registerUser.Email }, subject, body);

        //                //try
        //                //{
        //                //    _emailService.SendEmail(message);
        //                //}
        //                //catch (Exception ex)
        //                //{
        //                //    _commonService.ErrorLogs(ex.StackTrace, "Registration Mail", 1, ex.Message, ex.ToString());
        //                //}
        //            }
        //            return response;
        //        }
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateUserWithTokenAsync", 1, ex.Message, ex.ToString());
        //        response.IsSuccess = false;
        //        response.Message = _ApiResponseMessageList.FailResponseMessage;
        //        return response;
        //    }
        //}

        #endregion

        #region login
        public async Task<Response> LoginAsync(LoginModel loginModel)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginModel.Email);
                if (user == null)
                {
                    return ResponseHelper.BadRequest(_ApiResponseMessageList.InvalidEmailOrPassword);
                }
                if (user.IsDeleted)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);
                }

                if (!user.IsActive)
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.UserActiveStatusMessage);
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, loginModel.Password, false, false);
                if (!result.Succeeded)
                {
                    return ResponseHelper.BadRequest(_ApiResponseMessageList.InvalidEmailOrPassword);
                }

                var userRole = await _roleManager.FindByIdAsync(user.UserRoleId.ToString());
                var userRoleName = userRole?.Name ?? string.Empty;
                var roleId = userRole?.Id;

                return await CompleteLoginAsync(userRoleName, roleId, loginModel);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "LoginAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        private async Task<Response> CompleteLoginAsync(string userRoleName, string roleId, LoginModel loginModel)
        {
            Response response = new Response();
            try
            {
                var user = await _userManager.FindByEmailAsync(loginModel.Email);
                if (user == null)
                {
                    return ResponseHelper.BadRequest(_ApiResponseMessageList.InvalidEmailOrPassword);
                }

                var authClaim = new List<Claim>
                {
                    new Claim("UserId", user.Id),
                    new Claim(ClaimTypes.Email, user.UserName ?? ""),
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, userRoleName),
                    new Claim("UserType", userRoleName),
                    new Claim("ExpireDate", DateTime.UtcNow.ToString())
                };

                var jwtToken = CommonFunction.GetAccessToken(authClaim);
                var refreshToken = CommonFunction.GetRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);
                await _userManager.UpdateAsync(user);

                var result = new
                {
                    UserId = user.Id,
                    Token = jwtToken,
                    UserTypeName = userRoleName,
                    UserTypeId = roleId,
                    RefreshToken = refreshToken,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.EmailConfirmed,
                    UserProfileImage = string.IsNullOrEmpty(user.UserProfileImage)
                                                  ? null
                                                  : await _storageService.GetPresignedUrl(user.UserProfileImage, 1), //  Settings.AWS_S3_URL() + user.UserProfileImage,
                    UserRole = userRoleName,
                    user.IsActive
                };

                return ResponseHelper.Success(_ApiResponseMessageList.LoginSuccess, result, userRoleName);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CompleteLoginAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

		#endregion


		#region Refresh Token
		public async Task<Response> RefreshToken(RefreshTokenModel model)
        {
            Response response = new Response();
            try
            {
                var principal = GetPrincipalFromExpiredToken(model.AccessToken);
                if (principal == null || principal?.Identity?.Name is null)
                {
                    return ResponseHelper.InternalServerError(_ApiResponseMessageList.InvalidTokenMessage);
                }
                var user = await _userManager.FindByNameAsync(principal.Identity.Name);

                if (user is null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
                {
                    return ResponseHelper.InternalServerError(_ApiResponseMessageList.InvalidRefreshTokenMessage);
                }

                //var authClaim = new List<Claim>
                //{
                //    new Claim(ClaimTypes.Name, user.UserName),
                //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                //};

                var authClaim = new List<Claim>
                {
                    new Claim("UserId", user.Id),
                    new Claim(ClaimTypes.Email, user.UserName ?? ""),
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("ExpireDate", DateTime.UtcNow.ToString())
                };

                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    authClaim.Add(new Claim(ClaimTypes.Role, role));
                    new Claim("UserType", role);
                }

                var jwtToken = CommonFunction.GetAccessToken(authClaim);
                var refreshToken = CommonFunction.GetRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);

                await _userManager.UpdateAsync(user);

                TokenModel tokenModel = new TokenModel();
                tokenModel.Token = jwtToken;
                tokenModel.RefreshToken = refreshToken;

                return ResponseHelper.Success(_ApiResponseMessageList.RefreshTokenGeneratedMessage, tokenModel);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "RefreshToken", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var secret = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("Secret not configured");

                var validation = new TokenValidationParameters
                {
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = false
                };

                return new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        #endregion

        #region ForgotPassword
        public async Task<Response> ForgotPassword(ForgotPasswordModel forgotPasswordModel)
        {
            var response = new Response();
            try
            {
                var user = _userManager.FindByEmailAsync(forgotPasswordModel.Email).Result;
                if (user == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);
                }

                // If user status is not active then no need to proceed further
                if (!user.IsActive)
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.UserActiveStatusMessage);
                }

                var token = _userManager.GeneratePasswordResetTokenAsync(user).Result;
                var resetPasswordLink = Settings.GetAPIRootPath() + "/reset-password?email=" + user.Email + "&token=" + token;


                //try
                //{
                //    string body = string.Empty;
                //    string HTMLPath = Path.GetFullPath("Views/ForgotPasswordMail.html").Replace("~\\", "");
                //    using (StreamReader reader = new StreamReader(HTMLPath))
                //    {
                //        body = reader.ReadToEnd();
                //    }
                //    body = body.Replace("@MemberName", user.FirstName);
                //    body = body.Replace("@RestPasswordURL", resetPasswordLink);
                //    var message = new Message(new string[] { user.Email }, "[Part Finder " + Settings.GetEnvironment() + "] Reset Your Password", body);
                //    _emailService.SendEmail(message);
                //}
                //catch (Exception ex)
                //{
                //    _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "Registration Mail", 1, ex.Message, ex.ToString());
                //}

                return ResponseHelper.Success(_ApiResponseMessageList.ResetPasswordMailSendSuccessMessage, null);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ForgotPassword", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
        #endregion


        #region Change Password
        public async Task<Response> ChangeUserPasswordFromMail(ChangePasswordModelFromMail model)
        {
            var response = new Response();

            try
            {
                #region Get User Email By Token
                var currentUserEmail = CommonFunction.GetEmailByTokenParameter(model.Token);
                #endregion


                var user = await _userManager.FindByEmailAsync(currentUserEmail);

                if (user == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);

                }
                // If user status is not active then no need to proceed further
                if (!user.IsActive)
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.UserActiveStatusMessage);
                }

                #region Re setting the new password

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.ConfirmedPassword);

                #endregion

                if (!result.Succeeded)
                {
                    return ResponseHelper.BadRequest(result.Errors.Select(e => e.Description).FirstOrDefault());
                }

                return ResponseHelper.Success(_ApiResponseMessageList.PasswordChangeMessage, result);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "ChangeUserPasswordFromMail", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

		public async Task<Response> ChangePassword(ChangePasswordModel model)
		{
			var response = new Response();

			try
			{
				#region Get User Email By Token
				var currentUserEmail = CommonFunction.GetUserDataByToken(ClaimTypes.Email);
				var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
				#endregion


				var user = await _userManager.FindByEmailAsync(currentUserEmail);

				if (user == null)
				{
					return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);

				}
				// If user status is not active then no need to proceed further
				if (!user.IsActive)
				{
					return ResponseHelper.Forbidden(_ApiResponseMessageList.UserActiveStatusMessage);
				}

				#region Checking the old password
				
				var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.CurrentPassword, false);
				if (!passwordCheck.Succeeded)
					return ResponseHelper.BadRequest(_ApiResponseMessageList.InvalidPasswordMessage);
				
				#endregion
				#region Re setting the new password

				var token = await _userManager.GeneratePasswordResetTokenAsync(user);
				var result = await _userManager.ResetPasswordAsync(user, token, model.ConfirmedPassword);

				#endregion

				if (!result.Succeeded)
				{
					return ResponseHelper.BadRequest(result.Errors.Select(e => e.Description).FirstOrDefault());
				}

				return ResponseHelper.Success(_ApiResponseMessageList.PasswordChangedMessage, result, currentUserRole);

			}
			catch (Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace ?? "", "ChangePassword", 1, ex.Message, ex.ToString());
				return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
			}
		}
		#endregion


		#region Display Profile
		public async Task<Response> GetUserInfo()
        {
            var response = new Response();

            try
            {
                #region Get User Email By Token
                var currentUserEmail = CommonFunction.GetUserDataByToken(ClaimTypes.Email);
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #endregion


                var user = await _userManager.FindByEmailAsync(currentUserEmail);
                if (user == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);
                }
                // If user status is not active then no need to proceed further
                if (!user.IsActive)
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.UserActiveStatusMessage);
                }

                DisplayUserProfileModel userModel = new DisplayUserProfileModel();
                userModel.FirstName = user.FirstName;
                userModel.LastName = user.LastName;
                userModel.Email = user.Email;
                userModel.ContactNumber = user.PhoneNumber;
                userModel.ProfileImageUrl = user.UserProfileImage;
                userModel.UserRole = currentUserRole;
                userModel.IsActive = user.IsActive;
                userModel.ProfileImageUrl = (user.UserProfileImage != null && user.UserProfileImage != "") ? await _storageService.GetPresignedUrl(user.UserProfileImage, 1) : "";
					
				return ResponseHelper.Success(_ApiResponseMessageList.UserFetchSuccessMessage, userModel, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "GetUserInfo", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
        #endregion

        #region Edit Profile
        public async Task<Response> UpdateUserInfo(CreateUserModel model)
        {
            var response = new Response();

            try
            {
                #region Get User Email By Token
                var currentUserEmail = CommonFunction.GetUserDataByToken(ClaimTypes.Email);
				var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
				#endregion

				var user = await _userManager.FindByEmailAsync(currentUserEmail);
                if (user == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);
                }
                // If user status is not active then no need to proceed further
                if (!user.IsActive)
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.UserActiveStatusMessage);
                }

                // Update the fields
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.ContactNumber;

				#region Checking for user photo

				if (model.RemoveProfileImage == true)
				{
					user.UserProfileImage = "";
				}
				else if (model.ProfileImage != null)
                {
					try
					{
						// Generate a unique file name
						var originalFileName = Path.GetFileNameWithoutExtension(model.ProfileImage.FileName);
						var extension = Path.GetExtension(model.ProfileImage.FileName);
						var uniqueFileName = $"{originalFileName}_{Guid.NewGuid()}{extension}";

						#region Checking for file validation that it is image or not
						string[] allowedExtensions = Settings.GetAllowedImageExtensions();
						var allowedContentTypes = Settings.GetAllowedImageContentTypes();

						var contentType = model.ProfileImage.ContentType?.ToLower();

						if (!allowedExtensions.Contains(extension) || !allowedContentTypes.Contains(contentType))
						{
							return ResponseHelper.BadRequest(_ApiResponseMessageList.ImageExtensionErrorMessage);
						}

						#endregion

						// S3 key (path inside bucket)
						var s3Key = Settings.AWS_S3_USER_IMAGE_FOLDER() + $"{uniqueFileName}";

						// Read the file into a stream
						using (var stream = new MemoryStream())
						{
							await model.ProfileImage.CopyToAsync(stream);
							stream.Position = 0;

							// Upload to S3
							bool isUploaded = await _storageService.UploadFileToS3Bucket(stream, s3Key, model.ProfileImage.ContentType);// CommonFunction.UploadFileToS3Bucket(stream, s3Key, model.ProfileImage.ContentType);

							if (isUploaded)
							{
								user.UserProfileImage = s3Key;
							}
						}
					}
					catch (Exception ex)
					{
						_commonService.ErrorLogs(ex.StackTrace ?? "No stack trace", "User photo upload", 1, ex.Message, ex.ToString());
						user.UserProfileImage = "";
					}
				}
				#endregion

				var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return ResponseHelper.BadRequest(_ApiResponseMessageList.FailedToUpdateUserProfileMessage);
                }


                CreatedUserResponseModel _createdUserResponseModel = new CreatedUserResponseModel();
                _createdUserResponseModel.FirstName = user.FirstName;
                _createdUserResponseModel.LastName = user.LastName;
                _createdUserResponseModel.ContactNumber = user.PhoneNumber;
                _createdUserResponseModel.Email = model.Email;
				_createdUserResponseModel.UserProfileImage = (user.UserProfileImage != null && user.UserProfileImage != "") ? await _storageService.GetPresignedUrl(user.UserProfileImage, 1) : "";
                _createdUserResponseModel.Role = currentUserRole;

				return ResponseHelper.Success(_ApiResponseMessageList.UserProfileUpdatedMessage, _createdUserResponseModel, currentUserRole);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "UpdateUserInfo", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
        #endregion

        #region Logout
        public async Task<Response> Logout()
        {
            var response = new Response();

            try
            {
                var (token, expiry) = CommonFunction.GetToken();

                ExpiredTokens exp = new ExpiredTokens()
                {
                    AccessToken = token,
                    ExpiredDate = expiry
                };
                _context.Add(exp);

                await _context.SaveChangesAsync();

                return ResponseHelper.Success(_ApiResponseMessageList.LoginOutSuccessMessage);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "Logout", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        #endregion


        }
}
 