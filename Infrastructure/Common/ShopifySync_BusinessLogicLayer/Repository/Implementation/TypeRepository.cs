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
    public class TypeRepository : ITypeRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly ShopifySyncDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public TypeRepository(
            IConfiguration configuration,
            ShopifySyncDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<VehicleTypes>> GetAllVehicleTypes(string search, int pageNo, string pageSize)
        {
            IEnumerable<VehicleTypes> items = null;
            var query = _context.VehicleTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
            }

            if (pageSize != null && pageSize.ToLower() == "all")
            {
                items = await query
                        .OrderBy(x => x.VehicleTypesId)
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
                .OrderBy(x => x.VehicleTypesId)
                .ToPagedListAsync(pageNo, perPage);

            }

            return items;
        }

        public async Task<IEnumerable<VehicleTypes>> GetAllTypes()
        {
            return await _context.VehicleTypes
                .OrderBy(x => x.Name)
                .ToListAsync();
        }



        public async Task<VehicleTypes> GetVehicleTypeDetailsById(long id) =>
            await _context.VehicleTypes.FindAsync(id);

        public async Task<VehicleTypes> CreateIfNotExists(VehicleTypes candidate)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.Name))
            {
                throw new ArgumentException("Type name cannot be null or empty", nameof(candidate));
            }

            var name = candidate.Name.Trim();

            var existing = await _context.VehicleTypes
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                return existing;
            }

            var entity = new VehicleTypes
            {
                Name = name,
                SupplierId = candidate.SupplierId,
                ReferenceId = candidate.ReferenceId
            };

            _context.VehicleTypes.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task CreateNewVehicleType(VehicleTypes type)
        {
            _context.VehicleTypes.Add(type);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVehicleType(VehicleTypes type)
        {
            _context.VehicleTypes.Update(type);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleType(long typeId)
        {
            var type = await GetVehicleTypeDetailsById(typeId);
            if (type != null)
            {
                _context.VehicleTypes.Remove(type);
                await _context.SaveChangesAsync();
            }
        }
    }
}
