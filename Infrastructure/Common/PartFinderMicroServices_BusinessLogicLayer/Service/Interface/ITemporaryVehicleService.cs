using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TypeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PartFinderMicroServices_DataAccessLayer.Model;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.Supplier;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
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
