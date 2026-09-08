using Microsoft.AspNetCore.Http;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.ModelDTO;
using ShopifySync_DataAccessLayer.Entities.DTOs.VehicleDTO;
using System;
using static ShopifySync_DataAccessLayer.Entities.Vehicle.VehicleModel;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
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
