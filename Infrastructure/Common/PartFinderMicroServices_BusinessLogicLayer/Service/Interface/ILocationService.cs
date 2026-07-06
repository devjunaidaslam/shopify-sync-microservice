using System.Collections.Generic;
using System.Threading.Tasks;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface ILocationService
    {
        Task<Response> GetLocationsAsync(string search, int pageNo, string perPage);
    }
} 