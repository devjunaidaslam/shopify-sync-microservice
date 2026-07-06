using PartFinderMicroServices_DataAccessLayer.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IVendorRepository
    {
        Task<List<Vendor>> GetVendorsAsync(string search, int? pageNo, string perPage);
    }
} 