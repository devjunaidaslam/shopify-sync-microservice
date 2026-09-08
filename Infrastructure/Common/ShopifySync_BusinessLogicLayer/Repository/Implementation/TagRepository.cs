using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Model;
using ShopifySync_DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class TagRepository : ITagRepository
    {
        private readonly ShopifySyncDbContext _context;
        private readonly int DefaultPageSize = 20;

        public TagRepository(ShopifySyncDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tag>> GetTagsAsync(string search, int? pageNo, string perPage)
        {
            var query = _context.Tags.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Title.Contains(search));

            if (perPage != null && perPage.ToLower() == "all")
                return await query.OrderBy(t => t.Title).ToListAsync();

            int perPageInt = DefaultPageSize;

            if (!string.IsNullOrEmpty(perPage) && int.TryParse(perPage, out int parsed))
                perPageInt = parsed;
            int page = pageNo ?? 1;
            return await query.OrderBy(t => t.Title).Skip((page - 1) * perPageInt).Take(perPageInt).ToListAsync();
        }
    }
} 