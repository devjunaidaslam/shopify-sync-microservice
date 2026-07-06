using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PartFinderMicroServices_BusinessLogicLayer.Functions
{
    public class Settings
    {
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

        public static string GetEnvironment()
        { 
            var ENV = builder.Build().GetValue<string>("ENV:Environment");
            return ENV;
        }

        public static string GetAPIRootPath()
        {
            var path = builder.Build().GetValue<string>("AppSettings:APIRootPath");
            return path;
        }

        public static string EmailFrom()
        {
            var path = builder.Build().GetValue<string>("GoogleEmailConfiguration:From");
            return path;
        }
        public static string EmailSmtpServer()
        {
            var path = builder.Build().GetValue<string>("GoogleEmailConfiguration:SmtpServer");
            return path;
        }
        public static string EmailPort()
        {
            var path = builder.Build().GetValue<string>("GoogleEmailConfiguration:Port");
            return path;
        }
        public static string EmailUsername()
        {
            var path = builder.Build().GetValue<string>("GoogleEmailConfiguration:Username");
            return path;
        }
        public static string EmailPassword()
        {
            var path = builder.Build().GetValue<string>("GoogleEmailConfiguration:Password");
            return path;
        }
        public static string GetAwsAccessKey()
        {
            var AwsAccessKey = builder.Build().GetValue<string>("AppSettings:AWS_ACCESS_KEY");
            return AwsAccessKey;
        }

        public static string GetAWSSecretAccessKey()
        {
            var builder = Getbuilder();
            return builder.Build().GetValue<string>("AppSettings:AWS_SECRET_ACCESS_KEY");
        }

        public static string GetAWSUserImageAccessPath()
        {
            var builder = Getbuilder();
            return builder.Build().GetValue<string>("AppSettings:AWS_USER_IMAGE_ACCESS_PATH");
        }

         
        public static string AWSS3Bucket()
        {
            var builder = Getbuilder();
            return builder.Build().GetValue<string>("AppSettings:AWS_S3_BUCKET_PATH");
        }
		public static string AWS_S3_USER_IMAGE_FOLDER()
		{
			var builder = Getbuilder();
			return builder.Build().GetValue<string>("AppSettings:AWS_S3_USER_IMAGE_FOLDER");
		}

		public static string AWS_S3_PART_IMAGE_FOLDER()
		{
			var builder = Getbuilder();
			return builder.Build().GetValue<string>("AppSettings:AWS_S3_PART_IMAGE_FOLDER");
		}

		public static string AWS_S3_EXCEL_FOLDER()
		{
			var builder = Getbuilder();
			return builder.Build().GetValue<string>("AppSettings:AWS_S3_EXCEL_FOLDER");
		}
		public static string AWS_S3_URL()
		{
			var builder = Getbuilder();
			return builder.Build().GetValue<string>("AppSettings:AWS_S3_URL");
		}


		public static string PasswordUppercase()
        {
            var path = builder.Build().GetValue<string>("PasswordSettings:Uppercase");
            return path;
        }

        public static string PasswordLowercase()
        {
            var path = builder.Build().GetValue<string>("PasswordSettings:Lowercase");
            return path;
        }

        public static string PasswordDigits()
        {
            var path = builder.Build().GetValue<string>("PasswordSettings:Digits");
            return path;
        }

        public static string PasswordSymbols()
        {
            var path = builder.Build().GetValue<string>("PasswordSettings:Symbols");
            return path;
        }

        public static int OTPMin()
        {
            var path = builder.Build().GetValue<int>("OtpSettings:OTPMin");
            return path;
        }

        public static int OTPMax()
        {
            var path = builder.Build().GetValue<int>("OtpSettings:OTPMax");
            return path;
        }

		public static string[] GetAllowedImageExtensions()
		{
			var value = builder.Build().GetSection("FileUploadSettings:AllowedImageExtensions")
		                               .Get<string[]>();
			return value;

		}

		public static string[] GetAllowedImageContentTypes()
		{
			var value = builder.Build().GetSection("FileUploadSettings:AllowedImageContentTypes")
									   .Get<string[]>();
			return value;

		}

		public static string[] GetAllowedExcelExtensions()
		{
			var value = builder.Build().GetSection("FileUploadSettings:AllowedDocumentExtensions")
									   .Get<string[]>();
			return value;

		}
		public static string[] GetAllowedExcelContentTypes()
		{
			var value = builder.Build().GetSection("FileUploadSettings:AllowedDocumentContentTypes")
									   .Get<string[]>();
			return value;

		}
	}
}
