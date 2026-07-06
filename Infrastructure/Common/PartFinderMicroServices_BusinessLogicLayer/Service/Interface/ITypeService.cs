using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.YearDTO;
using PartFinderMicroServices_DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TypeDTO;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
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
