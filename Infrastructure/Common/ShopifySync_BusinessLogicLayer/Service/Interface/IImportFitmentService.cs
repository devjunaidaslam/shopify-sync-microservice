using ShopifySync_DataAccessLayer.Entities.DTOs.MakeDTO;
using ShopifySync_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Model;
using ShopifySync_DataAccessLayer.Entities.DTOs.ImportFitment;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IImportFitmentService
    {
        Task<Response> GetImportFitmentList(int pageNo, string pageSize);
        Task<Response> GetImportFitmentDetailsById(long id);
        Task<Response> CreateNewImportFitment(ImportFitmentCreateDTO dto);
        Task<Response> CreateNewImportFitmentWithFile(ImportFitmentFileUploadDTO fileUploadDto);
        Task<Response> UpdateImportFitment(ImportFitmentUpdateDTO dto);
        Task<Response> DeleteImportFitment(long id);
        
        // Import processing methods
        Task<Response> PreprocessImportFitment(long importFitmentId);
        Task<Response> FinalizeImportFitment(long importFitmentId);
        Task<Response> ProcessImportFitment(long importFitmentId);
    }
}
