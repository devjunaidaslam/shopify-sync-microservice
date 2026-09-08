using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;
using ShopifySync_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ShopifySync_DataAccessLayer.Model;
using ShopifySync_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;
using ShopifySync_DataAccessLayer.Entities.Supplier;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface ITemporaryVehicleService
    {
        Task<Response> GetTempVehicleList(TempVehicleFilterModel filterModel);
        Task<Response> GetTempVehicleDetailsById(long id);
        Task<Response> CreateNewTempVehicle(TemporaryVehicleCreateDTO dto);
        Task<Response> UpdateTempVehicle(TemporaryVehicleUpdateDTO dto);
        Task<Response> DeleteTempVehicle(long id);

        Task<Response> UploadFileToImportFitment(VehicleExcelFileModel model);

        Task<Response> ProcessFile();

        Task<Response> BulkUpdateVehicleAttribute(BulkUpdateVehicleAttributeDTO dto);
    }
}
