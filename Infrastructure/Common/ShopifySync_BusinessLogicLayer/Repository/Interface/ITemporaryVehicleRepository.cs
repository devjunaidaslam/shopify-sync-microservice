using Microsoft.AspNetCore.Http;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface ITemporaryVehicleRepository
    {
        Task<IEnumerable<TempVehicleImports>> GetTempVehicleList(TempVehicleFilterModel filterModel);
        Task<IEnumerable<TempVehicleImports>> GetTempVehicleList(int pageNo, string pageSize);
        Task<TempVehicleImports> GetTempVehicleDetailsById(long id);

        Task CreateNewTempVehicle(TempVehicleImports tempVehicle);

        Task BulkCreateTempVehicle(List<TempVehicleImports> listTempVehicle);
        Task BulkInsertTempVehicles(List<TempVehicleImports> tempVehicles);
        Task UpdateTempVehicle(TempVehicleImports tempVehicle);
        Task DeleteTempVehicle(long tempVehicleId);

        Task CreateNewImportFitment(ImportFitment importFitment);

       // Task ProcessFile(long tempVehicleId);

        Task<int> BulkUpdateVehicleAttribute(long tempVehicleImportId, string attributeType, string attributeValue, long? newAttributeId, bool setAsDefault);


    }
}
