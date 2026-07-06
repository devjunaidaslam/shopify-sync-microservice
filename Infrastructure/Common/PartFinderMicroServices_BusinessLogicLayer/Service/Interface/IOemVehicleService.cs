using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OEMVehicleDTO;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IOemVehicleService
    {
        Task<Response> AttachAsync(OemVehicleLinkDTO dto);
        Task<Response> DetachAsync(OemVehicleLinkDTO dto);
    }
}
