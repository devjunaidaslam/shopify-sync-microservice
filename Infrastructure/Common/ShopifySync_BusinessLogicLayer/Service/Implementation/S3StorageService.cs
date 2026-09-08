using Amazon.Runtime;
using Amazon.S3.Transfer;
using Amazon.S3;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_BusinessLogicLayer.Functions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_DataAccessLayer.Model;
using Amazon.S3.Model;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
	public class S3StorageService : IS3StorageService
	{
		#region Fields
		private readonly ICommonService _commonService;
		#endregion

		#region Constructor
		public S3StorageService(ICommonService commonService)
		{
			_commonService = commonService;
		}
		#endregion


		public async Task<MemoryStream> GetFileFromS3Bucket(string fileName)
		{
			try
			{
				var (bucketName, s3Client) = CommonFunction.GetS3BucketNameAndCredentials();

				var request = new Amazon.S3.Model.GetObjectRequest
				{
					BucketName = bucketName,
					Key = fileName
				};
				
				using (GetObjectResponse response = await s3Client.GetObjectAsync(request))
				using (var responseStream = response.ResponseStream)
				{
					var memoryStream = new MemoryStream();
					await responseStream.CopyToAsync(memoryStream);
					memoryStream.Position = 0; // reset position so it can be read later
					return memoryStream;
				}
			}
			catch (AmazonS3Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "GetFileFromS3Bucket", 1, ex.Message, ex.ToString());
				return null;
			}
			catch (Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "GetFileFromS3Bucket", 1, ex.Message, ex.ToString());
				return null;
			}
		}

		public async Task<string> GetPresignedUrl(string fileName, int expiresInDays = 1)
		{
			var (bucketName, s3Client) = CommonFunction.GetS3BucketNameAndCredentials();

			var request = new GetPreSignedUrlRequest
			{
				BucketName = bucketName,
				Key = fileName,
				Expires = DateTime.UtcNow.AddDays(expiresInDays),
				Verb = HttpVerb.GET
			};

			return s3Client.GetPreSignedURL(request);
		}
		public async Task<bool> UploadFileToS3Bucket(Stream stream, string key, string contentType)
		{
			try
			{
				var (bucketName, s3Client) = CommonFunction.GetS3BucketNameAndCredentials();

				var fileTransferUtility = new TransferUtility(s3Client);

				await fileTransferUtility.UploadAsync(stream, bucketName, key);

				return true;
			}
			catch (AmazonS3Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "UploadFileToS3Bucket", 1, ex.Message, ex.ToString());
				return false;
			}
			catch (Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "UploadFileToS3Bucket", 1, ex.Message, ex.ToString());
				return false;
			}
		}

		public async Task<bool> DeleteFileFromS3Bucket(string fileName)
		{
			try
			{
				var (bucketName, s3Client) = CommonFunction.GetS3BucketNameAndCredentials();

				var deleteRequest = new Amazon.S3.Model.DeleteObjectRequest
				{
					BucketName = bucketName,
					Key = fileName
				};
				
				var response = await s3Client.DeleteObjectAsync(deleteRequest);

				return true;
			}
			catch (AmazonS3Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "DeleteFileFromS3Bucket", 1, ex.Message, ex.ToString());
				return false;
			}
			catch (Exception ex)
			{
				_commonService.ErrorLogs(ex.StackTrace, "DeleteFileFromS3Bucket", 1, ex.Message, ex.ToString());
				return false;
			}
		}

	}
}
