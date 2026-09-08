using ShopifySync_DataAccessLayer.Entities.DTOs.ImportFitment;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface IImportFitmentRepository
    {
        Task<IEnumerable<ImportFitment>> GetImportFitmentList(int pageNo, string pageSize);
        Task<ImportFitment> GetImportFitmentDetailsById(long id);

        Task CreateNewImportFitment(ImportFitment importFitment);
        Task UpdateImportFitment(ImportFitment importFitment);
        Task DeleteImportFitment(long importFitmentId);
        Task<IEnumerable<ImportFitment>> GetPendingImports();
        Task<IEnumerable<ImportFitment>> GetPreprocessedImports();
    }
}
