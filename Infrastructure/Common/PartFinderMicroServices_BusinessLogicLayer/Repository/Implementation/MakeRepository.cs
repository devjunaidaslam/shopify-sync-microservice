using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList.EF;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class MakeRepository : IMakeRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public MakeRepository(
            IConfiguration configuration,
            PartFinderDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<VehicleMakes>> GetAllVehicleMakes(string search, int pageNo, string pageSize)
        {
            IEnumerable<VehicleMakes> items = null;
            var query = _context.VehicleMakes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
            }

            if (pageSize != null && pageSize.ToLower() == "all")
            {
                items = await query
                        .OrderBy(x => x.VehicleMakeId)
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
                .OrderBy(x => x.VehicleMakeId)
                .ToPagedListAsync(pageNo, perPage);

            }

            return items;
        }

        public async Task<IEnumerable<VehicleMakes>> GetMakesByTypeIdAndYearId(long typeId, long yearId)
        {
            return await _context.Vehicles
                .Where(v => v.TypeId == typeId && v.YearId == yearId && v.IsActive == true)
                .Join(_context.VehicleMakes,
                      v => v.MakeId,
                      m => m.VehicleMakeId,
                      (v, m) => m)
                .Distinct()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<VehicleMakes> GetVehicleMakeDetailsById(long id) =>
            await _context.VehicleMakes.FindAsync(id);

        public async Task<VehicleMakes> CreateIfNotExists(VehicleMakes candidate)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.Name))
            {
                throw new ArgumentException("Make name cannot be null or empty", nameof(candidate));
            }

            var name = candidate.Name.Trim();

            var existing = await _context.VehicleMakes
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                return existing;
            }

            var entity = new VehicleMakes
            {
                Name = name,
                SupplierId = candidate.SupplierId,
                ReferenceId = candidate.ReferenceId
            };

            _context.VehicleMakes.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task CreateNewVehicleMake(VehicleMakes make)
        {
            _context.VehicleMakes.Add(make);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVehicleMake(VehicleMakes make)
        {
            _context.VehicleMakes.Update(make);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleMake(long makeId)
        {
            var make = await GetVehicleMakeDetailsById(makeId);
            if (make != null)
            {
                _context.VehicleMakes.Remove(make);
                await _context.SaveChangesAsync();
            }
        }
    }
}
