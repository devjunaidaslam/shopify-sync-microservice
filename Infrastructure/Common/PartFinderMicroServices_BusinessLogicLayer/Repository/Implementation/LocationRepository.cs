using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;
using PartFinder_DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class LocationRepository : ILocationRepository
    {
        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize = 20;

        public LocationRepository(PartFinderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of locations based on search and pagination parameters.
        /// </summary>
        /// <param name="search">Search term for filtering locations.</param>
        /// <param name="pageNo">Page number for pagination (nullable).</param>
        /// <param name="perPage">Number of items per page or "all" for all items.</param>
        /// <returns>A list of locations matching the criteria.</returns>
        public async Task<List<Location>> GetLocationsAsync(string search, int? pageNo, string perPage)
        {
            var query = _context.Locations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l => l.Name.Contains(search));
            if (perPage != null && perPage.ToLower() == "all")
                return await query.OrderBy(l => l.Name).ToListAsync();
            int perPageInt = DefaultPageSize;
            if (!string.IsNullOrEmpty(perPage) && int.TryParse(perPage, out int parsed))
                perPageInt = parsed;
            int page = pageNo ?? 1;
            return await query.OrderBy(l => l.Name).Skip((page - 1) * perPageInt).Take(perPageInt).ToListAsync();
        }
    }
} 