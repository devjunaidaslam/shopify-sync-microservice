using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using ShopifySync_BusinessLogicLayer.Service.Interface;

namespace ShopifySync_BusinessLogicLayer.Functions
{
    public class CommonFunction
    {
        private static ICommonService _commonService;
        

        private static IAmazonS3 s3Client;

        private static IHttpContextAccessor HttpContextAccessor;
        private static IConfigurationBuilder builder = Getbuilder();
        public static IConfigurationBuilder Getbuilder()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json")
              .AddJsonFile($"appsettings.{env}.json", optional: true);
            return builder;
        }

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;

        }

        public static string GetAccessToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Build().GetValue<string>("JwtSettings:Secret")));

            var token = new JwtSecurityToken(
                      issuer: builder.Build().GetValue<string>("JwtSettings:Issuer"),
                      audience: builder.Build().GetValue<string>("JwtSettings:Audience"),
                      expires: DateTime.UtcNow.AddDays(1),
                      claims: authClaims,
                      signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
               );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenString;
        }

        public static string GetRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public static string GenerateSixDigitOTP()
        {
            int min = Settings.OTPMin();
            int max = Settings.OTPMax();

            Random random = new Random();
            return random.Next(min, max + 1).ToString();
        }

        public static bool CheckImageExist(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace, "CheckFileExists", 1, ex.Message, ex.ToString());
                return false;
            }
        }

        public static async Task<string> GetDestinationPathAsync(string basePath, string filepath, int type)
        {
            var result = "";
            try
            {
                var segments = filepath.Split(Path.DirectorySeparatorChar);
                var lastUrl = segments.Last();
                string ext = Path.GetExtension(lastUrl);
                var filename = Path.GetFileNameWithoutExtension(lastUrl);
                var uploadPath = Path.Combine(basePath, lastUrl);

                string destinationPath = type switch
                {
                    1 => Path.Combine(basePath, "UserProfile", "Original", filename + ext),
                    _ => Path.Combine(basePath, "UserProfile", "Original", filename + ext),
                };

                result = destinationPath;

                if (File.Exists(uploadPath))
                {
                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }
                    File.Move(uploadPath, destinationPath, true);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving file: {ex.Message}");
            }

            return result;
        }

        public static string GenerateStrongPassword(int length = 8)
        {
            string upper = Settings.PasswordUppercase();
            string lower = Settings.PasswordLowercase();
            string digits = Settings.PasswordDigits();
            string symbols = Settings.PasswordSymbols();
            string allChars = upper + lower + digits + symbols;

            var random = new Random();
            var password = new List<char>();

            // Ensure at least one of each type
            password.Add(upper[random.Next(upper.Length)]);
            password.Add(lower[random.Next(lower.Length)]);
            password.Add(digits[random.Next(digits.Length)]);
            password.Add(symbols[random.Next(symbols.Length)]);

            for (int i = password.Count; i < length; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the result so the guaranteed characters aren't always in the same place
            return new string(password.OrderBy(x => random.Next()).ToArray());

        }
		public static (string, DateTime) GetToken()
		{
			try
			{
                var token = HttpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

				var jwtHandler = new JwtSecurityTokenHandler();
				

				var jwtToken = jwtHandler.ReadJwtToken(token);
				var expiry = jwtToken.ValidTo;


                return (token, expiry);
			}
			catch (Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "GetUserDataByToken", 1, ex.Message, ex.ToString());
				return (null,DateTime.UtcNow);
			}
		}
		public static string GetUserDataByToken(string claimType)
        {
            var userData = "";
            try
            {
                string tokenString = HttpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();

                if (tokenString.StartsWith("Bearer "))
                {
                    tokenString = tokenString.Substring("Bearer ".Length).Trim();
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(tokenString);
                userData = jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;


                return userData ?? string.Empty;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace, "GetUserDataByToken", 1, ex.Message, ex.ToString());
                return "";
            }
        }


        public static string GetEmailByTokenParameter(string token)
        {
            var userEmail = "";
            try
            {
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                userEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                return userEmail ?? string.Empty;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace, "GetEmailByTokenParameter", 1, ex.Message, ex.ToString());
                return "";
            }
        }


        public static string GetTextSearch(string seachValue)
        {
            var searchvalue = "";
            try
            {
               
                #region Search 
                if (seachValue.IsNotNullOrEmpty())
                {
                    searchvalue = string.Empty;
                    var splits = seachValue.Split(' ');
                    foreach (var split in splits)
                    {
                        if (split.IsNullOrEmpty())
                        {
                            searchvalue = "* ";
                            continue;
                        }
                        if (searchvalue.IsNullOrEmpty())
                        {
                            searchvalue = string.Concat("*", split, "*");
                        }
                        else
                        {
                            searchvalue = string.Concat(searchvalue, " ", "*", split, "*");
                        }
                    }

                }
                else
                {
                    searchvalue = "*";
                }
                #endregion

                return searchvalue;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace, "GetTextSearch", 1, ex.Message, ex.ToString());
                return "";
            }
        }


      public static Func<string, string, string, HttpClient>? HttpClientFactoryOverride { get; set; }

      public  static HttpClient ConfigureShopifyHttpClient( string shopUrl, string token, string version)
        {
            if (HttpClientFactoryOverride != null)
            {
                return HttpClientFactoryOverride(shopUrl, token, version);
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri($"https://{shopUrl}/admin/api/{version}/graphql.json");
            client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;
        }


        #region Amazon S3 Bucket
        public static (string,AmazonS3Client) GetS3BucketNameAndCredentials()
        {

            try
            { 
                string bucketName = Settings.AWSS3Bucket();

                BasicAWSCredentials credentials = new BasicAWSCredentials(Settings.GetAwsAccessKey(), Settings.GetAWSSecretAccessKey());

                AmazonS3Client s3Client = new AmazonS3Client(credentials, Amazon.RegionEndpoint.CACentral1);

				return (bucketName, s3Client);
            }
            catch (AmazonS3Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace, "GetS3BucketNameAndCredentials", 1, ex.Message, ex.ToString());
                return (null,null);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace, "GetS3BucketNameAndCredentials", 1, ex.Message, ex.ToString());
				return (null, null);
			}
        }

        #endregion

    }
}
