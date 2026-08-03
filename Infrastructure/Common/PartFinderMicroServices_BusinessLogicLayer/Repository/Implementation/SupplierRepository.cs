using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;
using X.PagedList.EF;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class SupplierRepository : ISupplierRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public SupplierRepository(
            IConfiguration configuration,
            PartFinderDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<Suppliers>> GetAllSupplierList(string search, int pageNo, string pageSize)
        {
            IEnumerable<Suppliers> items = null;
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v => v.Name.ToLower().Contains(search.ToLower()));
            }

            if (pageSize != null && pageSize.ToLower() == "all")
            {
                items = await query.OrderBy(v => v.Name).ToListAsync();
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
                        .OrderBy(v => v.Name)
                        .ToPagedListAsync(pageNo, perPage);

            }

            return items;
        }



        public async Task<Suppliers> GetSupplierDetailsById(long id) =>
            await _context.Suppliers.FindAsync(id);

        public async Task<Suppliers?> GetSupplierByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return await _context.Suppliers
                .Where(s => s.Code != null && s.Code.ToUpper() == code.ToUpper())
                .FirstOrDefaultAsync();
        }

        public async Task CreateNewSupplier(Suppliers supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSupplier(Suppliers supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSupplier(long supplierId)
        {
            var supplier = await GetSupplierDetailsById(supplierId);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
        }
    }
}
