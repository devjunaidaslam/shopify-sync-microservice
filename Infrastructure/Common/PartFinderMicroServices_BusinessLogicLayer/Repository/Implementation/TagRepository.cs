using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;
using PartFinder_DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class TagRepository : ITagRepository
    {
        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize = 20;

        public TagRepository(PartFinderDbContext context)
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