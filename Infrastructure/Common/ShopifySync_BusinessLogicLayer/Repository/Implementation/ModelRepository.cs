using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList.EF;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class ModelRepository : IModelRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly ShopifySyncDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public ModelRepository(
            IConfiguration configuration,
            ShopifySyncDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<VehicleModels>> GetAllVehicleModels(string search, int pageNo, string pageSize)
        {
            IEnumerable<VehicleModels> items = null;
            var query = _context.VehicleModels.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
            }

            if (pageSize != null && pageSize.ToLower() == "all")
            {
                items = await query
                        .OrderBy(x => x.VehicleModelId)
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
                .OrderBy(x => x.VehicleModelId)
                .ToPagedListAsync(pageNo, perPage);

            }

            return items;
        }

        public async Task<IEnumerable<VehicleModels>> GetModelsByTypeIdYearIdAndMakeId(long typeId, long yearId, long makeId)
        {
            return await _context.Vehicles
                .Where(v => v.TypeId == typeId && v.YearId == yearId && v.MakeId == makeId && v.IsActive == true)
                .Join(_context.VehicleModels,
                      v => v.ModelId,
                      m => m.VehicleModelId,
                      (v, m) => m)
                .Distinct()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<VehicleModels> GetVehicleModelDetailsById(long id) =>
            await _context.VehicleModels.FindAsync(id);

        public async Task<VehicleModels> CreateIfNotExists(VehicleModels candidate)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.Name))
            {
                throw new ArgumentException("Model name cannot be null or empty", nameof(candidate));
            }

            var name = candidate.Name.Trim();

            var existing = await _context.VehicleModels
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                return existing;
            }

            var entity = new VehicleModels
            {
                Name = name,
                SupplierId = candidate.SupplierId,
                ReferenceId = candidate.ReferenceId
            };

            _context.VehicleModels.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task CreateNewVehicleModel(VehicleModels model)
        {
            _context.VehicleModels.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVehicleModel(VehicleModels model)
        {
            _context.VehicleModels.Update(model);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleModel(long modelId)
        {
            var model = await GetVehicleModelDetailsById(modelId);
            if (model != null)
            {
                _context.VehicleModels.Remove(model);
                await _context.SaveChangesAsync();
            }
        }
    }
}
