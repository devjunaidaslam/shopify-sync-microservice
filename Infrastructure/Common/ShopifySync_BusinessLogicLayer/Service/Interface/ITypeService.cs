using ShopifySync_DataAccessLayer.Entities.DTOs.YearDTO;
using ShopifySync_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface ITypeService
    {
        Task<Response> GetAllVehicleType(string search, int pageNo, string pageSize);
        Task<Response> GetVehicleTypeDetailsId(long id);
        Task<Response> CreateNewVehicleType(TypeCreateDTO dto);
        Task<Response> UpdateVehicleType(TypeUpdateDTO dto);
        Task<Response> DeleteVehicleType(long id);
    }
}
