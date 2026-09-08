using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;
using ShopifySync_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.DTOs.MakeDTO;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IMakeService
    {
        Task<Response> GetAllVehicleMakes(string search, int pageNo, string pageSize);
        Task<Response> GetVehicleMakeDetailsById(long id);
        Task<Response> CreateNewVehicleMake(MakeCreateDTO dto);
        Task<Response> UpdateVehicleMake(MakeUpdateDTO dto);
        Task<Response> DeleteVehicleMake(long id);
    }
}
