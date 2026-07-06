using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.MakeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PartFinderMicroServices_DataAccessLayer.Model;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ImportFitment;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
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
