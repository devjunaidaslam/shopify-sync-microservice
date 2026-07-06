using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IS3StorageService
    {
		Task<MemoryStream> GetFileFromS3Bucket(string key);

        Task<string> GetPresignedUrl(string fileName, int expiresInDays);
		Task<bool> UploadFileToS3Bucket(Stream stream, string key, string contentType);
        Task<bool> DeleteFileFromS3Bucket(string key);
        
    }
}
