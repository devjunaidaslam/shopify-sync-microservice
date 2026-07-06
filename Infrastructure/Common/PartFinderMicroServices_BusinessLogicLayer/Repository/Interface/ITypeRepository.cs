using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface ITypeRepository
    {
        Task<IEnumerable<VehicleTypes>> GetAllVehicleTypes(string search, int pageNo, string pageSize);
        Task<VehicleTypes> GetVehicleTypeDetailsById(long id);
        Task<IEnumerable<VehicleTypes>> GetAllTypes();

        // Creates a type if it doesn't exist by name (case-insensitive), returns existing or newly created entity
        Task<VehicleTypes> CreateIfNotExists(VehicleTypes candidate);

        Task CreateNewVehicleType(VehicleTypes type);
        Task UpdateVehicleType(VehicleTypes type);
        Task DeleteVehicleType(long typeId);
    }
}
