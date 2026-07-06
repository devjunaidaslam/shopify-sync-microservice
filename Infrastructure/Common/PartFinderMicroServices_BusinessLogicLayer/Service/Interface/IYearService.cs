using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.YearDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IYearService
    {
        Task<Response> GetAllVehicleYears(string search, int pageNo, string pageSize);
        Task<Response> GetVehicleYearDetailsById(long id);
        Task<Response> CreateNewVehicleYear(YearCreateDTO dto);
        Task<Response> UpdateVehicleYear(YearUpdateDTO dto);
        Task<Response> DeleteVehicleYear(long id);
    }
}
