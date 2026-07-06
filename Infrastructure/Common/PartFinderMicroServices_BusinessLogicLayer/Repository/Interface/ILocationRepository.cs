using PartFinderMicroServices_DataAccessLayer.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface ILocationRepository
    {
        Task<List<Location>> GetLocationsAsync(string search, int? pageNo, string perPage);
    }
} 