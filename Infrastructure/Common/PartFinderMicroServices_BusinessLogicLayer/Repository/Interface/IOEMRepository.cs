using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OEMDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IOEMRepository
    {
        Task<IEnumerable<OEMDTO>> GetAllOEMList(OEMFilterModel oemFilterModel);
        Task<OEMDTO> GetOEMDetailsById(int id);
        Task<OEM> CreateNewOEM(OEMCreateDTO dto);
        Task<OEM> UpdateOEM(OEMUpdateDTO dto);
        Task<bool> DeleteOEM(int id);
        Task<bool> OEMExists(int id);
        Task<bool> OEMNameExists(string name, int? excludeId = null);
        Task<OEM> CreateIfNotExists(string oemName);
    }
}
