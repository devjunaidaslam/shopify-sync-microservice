using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.OEMVehicleDTO;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IOemVehicleService
    {
        Task<Response> AttachAsync(OemVehicleLinkDTO dto);
        Task<Response> DetachAsync(OemVehicleLinkDTO dto);
    }
}
