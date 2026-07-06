using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;
using PartFinder_DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class VendorRepository : IVendorRepository
    {
        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize = 20;

        public VendorRepository(PartFinderDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vendor>> GetVendorsAsync(string search, int? pageNo, string perPage)
        {
            var query = _context.Vendors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(v => v.Title.Contains(search));

            if (perPage != null && perPage.ToLower() == "all")
                return await query.OrderBy(v => v.Title).ToListAsync();

            int perPageInt = DefaultPageSize;

            if (!string.IsNullOrEmpty(perPage) && int.TryParse(perPage, out int parsed))
                perPageInt = parsed;
            int page = pageNo ?? 1;
            return await query.OrderBy(v => v.Title).Skip((page - 1) * perPageInt).Take(perPageInt).ToListAsync();
        }
    }
} 