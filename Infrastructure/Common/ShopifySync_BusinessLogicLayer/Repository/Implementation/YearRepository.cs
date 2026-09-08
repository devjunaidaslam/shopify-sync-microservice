using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;
using X.PagedList.EF;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class YearRepository : IYearRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;


        private readonly ShopifySyncDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public YearRepository(
            IConfiguration configuration,
            ShopifySyncDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<VehicleYears>> GetAllVehicleYears(string search, int pageNo, string pageSize)
        {
            IEnumerable<VehicleYears> items = null;
            var query = _context.VehicleYears.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
            }

            if (pageSize != null && pageSize.ToLower() == "all")
            {
                items = await query
                        .OrderBy(x => x.VehicleYearId)
                        .ToListAsync();
            }
            else
            {
                int perPage = DefaultPageSize;

                if (pageSize.IsNotNullOrEmpty())
                {
                    int.TryParse(pageSize, out perPage);
                }
                pageNo = pageNo > 0 ? pageNo : 1;

                items = await query
                .OrderBy(x => x.VehicleYearId)
                .ToPagedListAsync(pageNo, perPage);

            }

            return items;
        }

        public async Task<IEnumerable<VehicleYears>> GetYearsByTypeId(long typeId)
        {
            return await _context.Vehicles
                .Where(v => v.TypeId == typeId && v.IsActive == true)
                .Join(_context.VehicleYears,
                      v => v.YearId,
                      y => y.VehicleYearId,
                      (v, y) => y)
                .Distinct()
                .OrderBy(y => y.Name)
                .ToListAsync();
        }

        public async Task<VehicleYears> GetVehicleYearDetailsById(long id) =>
            await _context.VehicleYears.FindAsync(id);

        public async Task<VehicleYears> CreateIfNotExists(VehicleYears candidate)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.Name))
            {
                throw new ArgumentException("Year name cannot be null or empty", nameof(candidate));
            }

            var name = candidate.Name.Trim();

            var existing = await _context.VehicleYears
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                return existing;
            }

            var entity = new VehicleYears
            {
                Name = name,
                SupplierId = candidate.SupplierId,
                ReferenceId = candidate.ReferenceId
            };

            _context.VehicleYears.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task CreateNewVehicleYear(VehicleYears year)
        {
            _context.VehicleYears.Add(year);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVehicleYear(VehicleYears year)
        {
            _context.VehicleYears.Update(year);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleYear(long yearId)
        {
            var year = await GetVehicleYearDetailsById(yearId);
            if (year != null)
            {
                _context.VehicleYears.Remove(year);
                await _context.SaveChangesAsync();
            }
        }
    }
}
