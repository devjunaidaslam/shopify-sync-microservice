using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IYearRepository
    {
        Task<IEnumerable<VehicleYears>> GetAllVehicleYears(string search, int pageNo, string pageSize);
        Task<VehicleYears> GetVehicleYearDetailsById(long id);
        Task<IEnumerable<VehicleYears>> GetYearsByTypeId(long typeId);

        // Creates a year if it doesn't exist by name (case-insensitive), returns existing or newly created entity
        Task<VehicleYears> CreateIfNotExists(VehicleYears candidate);

        Task CreateNewVehicleYear(VehicleYears year);
        Task UpdateVehicleYear(VehicleYears year);
        Task DeleteVehicleYear(long yearId);
    }
}
