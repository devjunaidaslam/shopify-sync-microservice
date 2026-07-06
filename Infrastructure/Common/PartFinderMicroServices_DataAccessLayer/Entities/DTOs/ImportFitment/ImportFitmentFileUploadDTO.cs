using Microsoft.AspNetCore.Http;
using System;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ImportFitment
{
    public class ImportFitmentFileUploadDTO
    {
        public long SupplierId { get; set; }
        public bool IsDefault { get; set; }
        public IFormFile File { get; set; }
    }
}
