using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IMakeRepository
    {
        Task<IEnumerable<VehicleMakes>> GetAllVehicleMakes(string search, int pageNo, string pageSize);
        Task<VehicleMakes> GetVehicleMakeDetailsById(long id);
        Task<IEnumerable<VehicleMakes>> GetMakesByTypeIdAndYearId(long typeId, long yearId);

        // Creates a make if it doesn't exist by name (case-insensitive), returns existing or newly created entity
        Task<VehicleMakes> CreateIfNotExists(VehicleMakes candidate);

        Task CreateNewVehicleMake(VehicleMakes type);
        Task UpdateVehicleMake(VehicleMakes type);
        Task DeleteVehicleMake(long typeId);
    }
}
