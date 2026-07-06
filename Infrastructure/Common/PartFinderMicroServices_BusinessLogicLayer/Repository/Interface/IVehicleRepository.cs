using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PartFinderMicroServices_DataAccessLayer.Entities.Vehicle.VehicleModel;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IVehicleRepository
    {
        Task<IEnumerable<VehicleDTO>> GetAllVehicleList(VehicleFilterModel vehicleFilterModel);
        Task<VehicleDTO> GetVehicleDetailsById(long id); 
        Task<Vehicles> GetVehicleDetailsByIdForDelete(long id);

        Task CreateNewVehicle(Vehicles vehicle);
        Task UpdateVehicle(Vehicles vehicle);
        Task DeleteVehicle(long vehicleId);
        
        // Get all vehicle components
        Task<IEnumerable<VehicleTypes>> GetAllVehicleTypes();
        Task<IEnumerable<VehicleYears>> GetAllVehicleYears();
        Task<IEnumerable<VehicleMakes>> GetAllVehicleMakes();
        Task<IEnumerable<VehicleModels>> GetAllVehicleModels();

        // Hierarchy navigation methods
        Task<IEnumerable<VehicleYears>> GetYearsByTypeId(long typeId);
        Task<IEnumerable<VehicleMakes>> GetMakesByTypeIdAndYearId(long typeId, long yearId);
        Task<IEnumerable<VehicleModels>> GetModelsByTypeIdYearIdAndMakeId(long typeId, long yearId, long makeId);
        
        // Vehicle lookup
        Task<Vehicles> GetVehicle(long typeId, long makeId, long yearId, long modelId);

        // Component creation methods
        Task<VehicleTypes> CreateVehicleType(VehicleTypes type);
        Task<VehicleYears> CreateVehicleYear(VehicleYears year);
        Task<VehicleMakes> CreateVehicleMake(VehicleMakes make);
        Task<VehicleModels> CreateVehicleModel(VehicleModels model);
        
        // Update resource active status
        Task<int> UpdateResourceActiveStatus(long resourceId, string resourceType, bool isActive);
    }
}
