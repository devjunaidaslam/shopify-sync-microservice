using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Model;
using ShopifySync_DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class CollectionRepository : ICollectionRepository
    {
        private readonly ShopifySyncDbContext _context;
        private readonly int DefaultPageSize = 20;

        public CollectionRepository(ShopifySyncDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of collections based on search and pagination parameters.
        /// </summary>
        /// <param name="search">Search term for filtering collections.</param>
        /// <param name="pageNo">Page number for pagination (nullable).</param>
        /// <param name="perPage">Number of items per page or "all" for all items.</param>
        /// <returns>A list of collections matching the criteria.</returns>
        public async Task<List<Collection>> GetCollectionsAsync(string search, int? pageNo, string perPage)
        {
            var query = _context.Collections.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Title.Contains(search));

            if (perPage != null && perPage.ToLower() == "all")
                return await query.OrderBy(c => c.Title).ToListAsync();

            int perPageInt = DefaultPageSize;

            if (!string.IsNullOrEmpty(perPage) && int.TryParse(perPage, out int parsed))
                perPageInt = parsed;
            int page = pageNo ?? 1;
            return await query.OrderBy(c => c.Title).Skip((page - 1) * perPageInt).Take(perPageInt).ToListAsync();
        }
    }
} 