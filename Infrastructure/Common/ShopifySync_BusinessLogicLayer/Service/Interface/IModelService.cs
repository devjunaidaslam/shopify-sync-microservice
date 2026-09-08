using ShopifySync_DataAccessLayer.Entities.DTOs.MakeDTO;
using ShopifySync_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.DTOs.ModelDTO;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IModelService
    {
        Task<Response> GetAllVehicleModels(string search, int pageNo, string pageSize);
        Task<Response> GetVehicleModelDetailsById(long id);
        Task<Response> CreateNewVehicleModel(ModelCreateDTO dto);
        Task<Response> UpdateVehicleModel(ModelUpdateDTO dto);
        Task<Response> DeleteVehicleModel(long id);
    }
}
