using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface IModelRepository
    {
        Task<IEnumerable<VehicleModels>> GetAllVehicleModels(string search, int pageNo, string pageSize);
        Task<VehicleModels> GetVehicleModelDetailsById(long id);
        Task<IEnumerable<VehicleModels>> GetModelsByTypeIdYearIdAndMakeId(long typeId, long yearId, long makeId);

        // Creates a model if it doesn't exist by name (case-insensitive), returns existing or newly created entity
        Task<VehicleModels> CreateIfNotExists(VehicleModels candidate);

        Task CreateNewVehicleModel(VehicleModels type);
        Task UpdateVehicleModel(VehicleModels type);
        Task DeleteVehicleModel(long typeId);
    }
}
