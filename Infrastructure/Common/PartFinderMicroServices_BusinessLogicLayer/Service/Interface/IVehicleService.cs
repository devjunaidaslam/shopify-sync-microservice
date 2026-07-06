using Microsoft.AspNetCore.Http;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ModelDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO;
using System;
using static PartFinderMicroServices_DataAccessLayer.Entities.Vehicle.VehicleModel;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IVehicleService
    {
        Task<Response> GetAllVehicleList(VehicleFilterModel vehicleFilterModel);
        Task<Response> GetVehicleDetailsById(long id);
        Task<Response> CreateNewVehicle(VehicleCreateDTO dto);
        Task<Response> UpdateVehicle(VehicleUpdateDTO dto);
        Task<Response> DeleteVehicle(long id);
        Task<Response> NavigateVehicleHierarchy(long? typeId, long? yearId, long? makeId);
        Task<Response> UpdateResourceActiveStatus(UpdateResourceActiveStatusDTO dto);
    }
}
