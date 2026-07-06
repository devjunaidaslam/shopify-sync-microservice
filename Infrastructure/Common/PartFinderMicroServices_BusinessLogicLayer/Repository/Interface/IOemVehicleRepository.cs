using PartFinderMicroServices_DataAccessLayer.Model;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IOemVehicleRepository
    {
        Task<OemVehicle> AttachAsync(int oemId, long vehicleId, long? supplierId);
        Task<bool> DetachAsync(int oemId, long vehicleId);
        Task<bool> ExistsAsync(int oemId, long vehicleId);
    }
}
