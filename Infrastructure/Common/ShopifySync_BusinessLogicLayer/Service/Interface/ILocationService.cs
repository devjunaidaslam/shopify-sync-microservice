using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface ILocationService
    {
        Task<Response> GetLocationsAsync(string search, int pageNo, string perPage);
    }
} 