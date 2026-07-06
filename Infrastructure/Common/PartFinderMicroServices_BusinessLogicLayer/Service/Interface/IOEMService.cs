using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OEMDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IOEMService
    {
        Task<Response> GetAllOEMList(OEMFilterModel oemFilterModel);
        Task<Response> GetOEMDetailsById(int id);
        Task<Response> CreateNewOEM(OEMCreateDTO dto);
        Task<Response> UpdateOEM(OEMUpdateDTO dto);
        Task<Response> DeleteOEM(int id);
    }
}
