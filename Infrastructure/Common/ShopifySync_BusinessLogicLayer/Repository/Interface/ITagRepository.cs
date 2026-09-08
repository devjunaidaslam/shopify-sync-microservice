using ShopifySync_DataAccessLayer.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface ITagRepository
    {
        Task<List<Tag>> GetTagsAsync(string search, int? pageNo, string perPage);
    }
} 