using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_DataAccessLayer.Entities.Authentication.Register;
using ShopifySync_DataAccessLayer.Entities.Email;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Enum;
using ShopifySync_DataAccessLayer.Model;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using System.Security.Claims;
using ShopifySync_DataAccessLayer.Entities.Display;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.EF;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class AdminService : IAdminService
    {

        #region Fields
        private readonly UserManager<AspNetUser> _userManager;
        private readonly SignInManager<AspNetUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IEmailService _emailService;
        private readonly IS3StorageService _storageService;

        private readonly ShopifySyncDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public AdminService(
             UserManager<AspNetUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<AspNetUser> signInManager,
            IConfiguration configuration,
            ICommonService commonService,
            IEmailService emailService,
			IS3StorageService storageService,
			ShopifySyncDbContext context
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

            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion


        #region Create New User
        public async Task<Response> AddNewUser(RegisterUser registerUser)
        {
            Response response = new Response();
            try
            {
                #region getting user role from token
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #endregion

                // checking user is admin or not
                if (currentUserRole.ToLower().Contains("admin"))
                {
                    // Check if the email is already registered before creating the user?
                    var userExists = await _userManager.FindByEmailAsync(registerUser.Email);
                    if (userExists != null)
                    {
                        return ResponseHelper.Conflict(_ApiResponseMessageList.AccountAlreadyExist);
                    }
                    else
                    {
                        // Creating the user with the role user
                       // var userRole = _roleManager.Roles.FirstOrDefault(r => r.Name == UserRole.User.GetEnumDisplayName());
                        var userRole = _roleManager.Roles.FirstOrDefault(r => r.Name.ToLower() == registerUser.Role.ToLower().ToString());
                        if (userRole == null)
                        {
                            return ResponseHelper.BadRequest(_ApiResponseMessageList.InvalidUserRoleMessage);
                        }


                        #region Checking for user photo
                        var userImage = "";
						if (registerUser.ProfileImage != null)
						{
							try
							{
								// Generate a unique file name
								var originalFileName = Path.GetFileNameWithoutExtension(registerUser.ProfileImage.FileName);
								var extension = Path.GetExtension(registerUser.ProfileImage.FileName);
								var uniqueFileName = $"{originalFileName}_{Guid.NewGuid()}{extension}";


								#region Checking for file validation that it is image or not
								var allowedExtensions = Settings.GetAllowedImageExtensions();
								var allowedContentTypes = Settings.GetAllowedImageContentTypes();

								var contentType = registerUser.ProfileImage.ContentType?.ToLower();

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
									await registerUser.ProfileImage.CopyToAsync(stream);
									stream.Position = 0;

                                    // Upload to S3
                                    bool isUploaded = await _storageService.UploadFileToS3Bucket(stream, s3Key, registerUser.ProfileImage.ContentType);// CommonFunction.UploadFileToS3Bucket(stream, s3Key, registerUser.ProfileImage.ContentType);

									if (isUploaded)
									{
										userImage =  s3Key;
									}
									else
									{
										userImage = "";
									}
								}
							}
							catch (Exception ex)
							{
								_commonService.ErrorLogs(ex.StackTrace ?? "No stack trace", "User photo upload", 1, ex.Message, ex.ToString());
								userImage = "";
							}
						}
                        #endregion


                        AspNetUser user = new()
                        {
                            Email = registerUser.Email,
                            SecurityStamp = new Guid().ToString(),
                            UserName = registerUser.Email,
                            FirstName = registerUser.FirstName,
                            LastName = registerUser.LastName,
                            PhoneNumber = registerUser.ContactNumber,
                            CreatedDate = DateTime.UtcNow,
                            UserRoleId = userRole.Id,
                            IsActive = true,
                            UserProfileImage = userImage
                        };


                        #region Creating default password for user
                        // Genarating 8 digit strong password for user
                        string defaultPassword = CommonFunction.GenerateStrongPassword(8);

                        var result = await _userManager.CreateAsync(user, defaultPassword);
						#endregion

                        if (!result.Succeeded)
                        {
                            return ResponseHelper.BadRequest(_ApiResponseMessageList.UserFailedToRegister);
                        }

						#region Sending password mail to user

						try
						{
							string body = string.Empty;
							string HTMLPath = Path.GetFullPath("Views/SendPasswordMail.html").Replace("~\\", "");
							using (StreamReader reader = new StreamReader(HTMLPath))
							{
								body = reader.ReadToEnd();
							}
							body = body.Replace("@MemberName", user.FirstName);
							body = body.Replace("@ChangedPassword", defaultPassword);
							var message = new Message(new string[] { user.Email }, "[Part Finder " + Settings.GetEnvironment() + "] Account Created", body);

							_emailService.SendEmail(message);
						}
						catch (Exception ex)
						{
							_commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "Account Creation Mail", 1, ex.Message, ex.ToString());
						}

						#endregion

						CreatedUserResponseModel _createdUserResponseModel = new CreatedUserResponseModel();
                        _createdUserResponseModel.FirstName = user.FirstName;
                        _createdUserResponseModel.LastName = user.LastName;
                        _createdUserResponseModel.Email = user.Email;
                        _createdUserResponseModel.Password = defaultPassword;
                        _createdUserResponseModel.Role = userRole.Name;
                        _createdUserResponseModel.ContactNumber = user.PhoneNumber;
                        _createdUserResponseModel.UserProfileImage = (userImage != null && userImage != "") ? await _storageService.GetPresignedUrl(userImage, 1) : "";


						return ResponseHelper.Success(_ApiResponseMessageList.UserCreateMessage, _createdUserResponseModel, currentUserRole);

                    }
                }
                else
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.ForbiddenAccessToCreateUserMessage);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
		#endregion

		#region Upload image to S3
		public async Task<Response> UploadUserPhotoToS3(IFormFile file)
		{
			
			#region getting user role from token
			var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
			#endregion
			try
			{
				// Generate a unique file name
				var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
				var extension = Path.GetExtension(file.FileName);
				var uniqueFileName = $"{originalFileName}_{Guid.NewGuid()}{extension}";

				// S3 key (path inside bucket)
				var s3Key = Settings.AWS_S3_USER_IMAGE_FOLDER() + $"{uniqueFileName}";

				// Read the file into a stream
				using (var stream = new MemoryStream())
				{
					await file.CopyToAsync(stream);
					stream.Position = 0;

					// Upload to S3
					bool isUploaded = await _storageService.UploadFileToS3Bucket(stream, s3Key, file.ContentType);

					if (isUploaded)
					{
						return ResponseHelper.Success(_ApiResponseMessageList.UserPhotoUploadMessage, await _storageService.GetPresignedUrl(s3Key, 1), currentUserRole);
						// Return the file's public S3 URL
						//return 
					}
					else
					{
						return null;
					}
				}
			}
			catch (Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace ?? "No stack trace", "UploadUserPhotoToS3", 1, ex.Message, ex.ToString());
				return ResponseHelper.BadRequest(_ApiResponseMessageList.UserFailedToRegister);
		
			}
		}
		#endregion

		#region Display All User Profile
		public async Task<Response> GetUsersList(string search, int pageNo, string pageSize)
        {
            var response = new Response();

            try
            {
                #region getting user role from token
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #endregion

                // checking user is admin or not
                if (currentUserRole.ToLower().Contains("admin"))
                {
                    var query = _context.Users
                                .Where(x => x.IsActive == true)
                                .Select(x => new DisplayUserProfileModel
                                {
               
                                    FirstName = x.FirstName,
                                    LastName = x.LastName,
                                    Email = x.Email,
                                    ContactNumber = x.PhoneNumber,
                                    ProfileImageUrl = x.UserProfileImage,
                                    UserRole = x.UserRoleId,
                                    IsActive = x.IsActive
                                });

                    #region Search 
                    if (!string.IsNullOrEmpty(search))
                    {
                        string searchTerm = search.ToLower();
                        query = query.Where(x =>
                            x.FirstName.ToLower().Contains(searchTerm) ||
                            x.LastName.ToLower().Contains(searchTerm) ||
                            x.Email.ToLower().Contains(searchTerm));
                    }
                    #endregion

                    var totalItemCount = await query.CountAsync();
                    int perPage = DefaultPageSize;

                    if (!string.IsNullOrEmpty(pageSize) && pageSize.ToLower() != "all")
                    {
                        int.TryParse(pageSize, out perPage);
                    }
                    else
                    {
                        perPage = totalItemCount;
                    }

                    pageNo = pageNo > 0 ? pageNo : 1;
                    var totalPages = perPage > 0 ? (int)Math.Ceiling((double)totalItemCount / perPage) : 0;

                    int nextPage = pageNo < totalPages ? pageNo + 1 : 0;
                    int prevPage = pageNo > 1 ? pageNo - 1 : 0;

                    List<DisplayUserProfileModel> userList;
                    if (!string.IsNullOrEmpty(pageSize) && pageSize.ToLower() == "all")
                    {
                        userList = await query.ToListAsync();
                    }
                    else
                    {
                        var pagedUsers = await query.ToPagedListAsync(pageNo, perPage);
                        userList = pagedUsers.ToList();
                    }

                    foreach (var user in userList)
                    {
                        user.UserRole = GetUserRoleName(user.UserRole);
						user.ProfileImageUrl = (user.ProfileImageUrl != null && user.ProfileImageUrl != "") ? await _storageService.GetPresignedUrl(user.ProfileImageUrl, 1) : "";
					}

                    var pagination = new PaginationInfo
                    {
                        TotalItemCount = totalItemCount,
                        PageNo = pageNo,
                        PerPage = perPage,
                        TotalPages = totalPages,
                        NextPage = nextPage,
                        PrevPage = prevPage
                    };

                    return ResponseHelper.Success(_ApiResponseMessageList.UserFetchSuccessMessage, userList, currentUserRole,pagination);
                }
                else
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.ForbiddenAccessToViewUserMessage);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "GetUsersList", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        private string GetUserRoleName(string roleId)
        {
            var role = _roleManager.Roles.FirstOrDefault(r => r.Id == roleId).Name;
            return role != null ? role : string.Empty;
        }
        #endregion

        #region Display Any User Profile
        public async Task<Response> GetUserInfoByUserEmail(string email)
        {
            var response = new Response();

            try
            {
                #region getting user role from token
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #endregion

                // checking user is admin or not
                if (currentUserRole.ToLower().Contains("admin"))
                {
                    // Checking if the user data exist for viewing
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);
                    }
                    
                    DisplayUserProfileModel userModel = new DisplayUserProfileModel();
                    userModel.FirstName = user.FirstName;
                    userModel.LastName = user.LastName;
                    userModel.Email = user.Email;
                    userModel.ContactNumber = user.PhoneNumber;
                    userModel.ProfileImageUrl = user.UserProfileImage;
                    userModel.UserRole = GetUserRoleName(user.UserRoleId);
                    userModel.IsActive = user.IsActive;
					userModel.ProfileImageUrl = (user.UserProfileImage != null && user.UserProfileImage != "") ? await _storageService.GetPresignedUrl(user.UserProfileImage, 1) : "";

					return ResponseHelper.Success(_ApiResponseMessageList.UserFetchSuccessMessage, userModel, currentUserRole);
                }
                else
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.ForbiddenAccessToViewUserMessage);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "GetUserInfoByUserEmail", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
        #endregion

        #region Edit Any User Profile
        public async Task<Response> UpdateUserInfoByUserEmail(CreateUserModel model)
        {
            var response = new Response();

            try
            {
                #region getting user role from token
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #endregion

                // checking user is admin or not
                if (currentUserRole.ToLower().Contains("admin"))
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        response.IsSuccess = false;
                        response.Message = _ApiResponseMessageList.UserNotFoundResponseMessage;
                        response.StatusCode = 404;
                        return response;
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
								bool isUploaded = await _storageService.UploadFileToS3Bucket(stream, s3Key, model.ProfileImage.ContentType);// CommonFunction.UploadFileToS3Bucket(stream, s3Key, registerUser.ProfileImage.ContentType);

								if (isUploaded)
								{
									user.UserProfileImage = s3Key;
								}
							}
						}
						catch (Exception ex)
						{
							_commonService.ErrorLogs(ex.StackTrace ?? "No stack trace", "User photo upload", 1, ex.Message, ex.ToString());
						}
					}
					#endregion

					
					var result = await _userManager.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        return ResponseHelper.BadRequest(_ApiResponseMessageList.FailedToUpdateUserProfileMessage);
                    }

                    var userRole = await _roleManager.FindByIdAsync(user.UserRoleId.ToString());
                    var userRoleName = userRole?.Name ?? string.Empty;

                    DisplayUserProfileModel userModel = new DisplayUserProfileModel();
                    userModel.FirstName = user.FirstName;
                    userModel.LastName = user.LastName;
                    userModel.Email = user.Email; 
                    userModel.ContactNumber = user.PhoneNumber;
                    userModel.ProfileImageUrl = (user.UserProfileImage != null && user.UserProfileImage != "") ? await _storageService.GetPresignedUrl(user.UserProfileImage, 1) : "";
                    userModel.UserRole = userRoleName;
                    userModel.IsActive = user.IsActive;

                    return ResponseHelper.Success(_ApiResponseMessageList.UserProfileUpdatedMessage, userModel, currentUserRole);
                }
                else
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.ForbiddenAccessToEditUserMessage);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "UpdateUserInfoByUserEmail", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        #endregion

        #region Active/Deactive User
          public async Task<Response> ActiveDeactiveUser(UserEmail model)
          {
            var response = new Response();

            try
            {
                #region getting user role from token
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #endregion

                // checking user is admin or not
                if (currentUserRole.ToLower().Contains("admin"))
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        return ResponseHelper.NotFound(_ApiResponseMessageList.UserNotFoundResponseMessage);
                    }

                    // Update the fields
                    user.IsActive = model.IsUserActive;
                    var result = await _userManager.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        return ResponseHelper.BadRequest(_ApiResponseMessageList.FailedToUpdateUserActiveStatusMessage);
                    }

                    var userRole = await _roleManager.FindByIdAsync(user.UserRoleId.ToString());
                    var userRoleName = userRole?.Name ?? string.Empty;

                    return ResponseHelper.Success(_ApiResponseMessageList.UpdateUserActiveStatusMessage, null, currentUserRole);
                }
                else
                {
                    return ResponseHelper.Forbidden(_ApiResponseMessageList.ForbiddenAccessToEditUserMessage);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "", "ActiveDeactiveUser", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        #endregion

    }
}
