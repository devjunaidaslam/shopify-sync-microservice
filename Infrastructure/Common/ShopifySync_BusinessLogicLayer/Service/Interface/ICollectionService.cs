using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface ICollectionService
    {
        Task<Response> GetCollectionsAsync(string search, int pageNo, string perPage);
    }
} 