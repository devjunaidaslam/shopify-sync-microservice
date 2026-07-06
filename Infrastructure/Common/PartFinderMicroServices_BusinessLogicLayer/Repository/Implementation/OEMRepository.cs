using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OEMDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList.EF;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class OEMRepository : IOEMRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public OEMRepository(
            IConfiguration configuration,
            PartFinderDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<OEMDTO>> GetAllOEMList(OEMFilterModel oemFilterModel)
        {
            IEnumerable<OEMDTO> items = null;
            var query = from oem in _context.OEMs
                        select new
                        {
                            oem
                        };

            if (!string.IsNullOrWhiteSpace(oemFilterModel.search))
            {
                query = query.Where(x =>
                    x.oem.Name.ToLower().Contains(oemFilterModel.search.ToLower()));
            }

            if (oemFilterModel.per_page != null && oemFilterModel.per_page.ToLower() == "all")
            {
                items = await query
                        .OrderBy(x => x.oem.Id)
                        .Select(x => new OEMDTO
                        {
                            Id = x.oem.Id,
                            Name = x.oem.Name
                        }).ToListAsync();
            }
            else
            {
                int perPage = DefaultPageSize;

                if (oemFilterModel.per_page.IsNotNullOrEmpty())
                {
                    int.TryParse(oemFilterModel.per_page, out perPage);
                }
                oemFilterModel.page_no = oemFilterModel.page_no > 0 ? oemFilterModel.page_no : 1;

                items = await query
                .OrderBy(x => x.oem.Id)
                .Select(x => new OEMDTO
                {
                    Id = x.oem.Id,
                    Name = x.oem.Name
                })
                .ToPagedListAsync((int)oemFilterModel.page_no, perPage);
            }

            return items;
        }

        public async Task<OEMDTO> GetOEMDetailsById(int id)
        {
            var oem = await _context.OEMs
                .Where(x => x.Id == id)
                .Select(x => new OEMDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .FirstOrDefaultAsync();

            return oem;
        }

        public async Task<OEM> CreateNewOEM(OEMCreateDTO dto)
        {
            var oem = new OEM
            {
                Name = dto.Name
            };

            _context.OEMs.Add(oem);
            await _context.SaveChangesAsync();
            return oem;
        }

        public async Task<OEM> UpdateOEM(OEMUpdateDTO dto)
        {
            var oem = await _context.OEMs.FindAsync(dto.Id);
            if (oem != null)
            {
                oem.Name = dto.Name;
                await _context.SaveChangesAsync();
            }
            return oem;
        }

        public async Task<bool> DeleteOEM(int id)
        {
            var oem = await _context.OEMs.FindAsync(id);
            if (oem != null)
            {
                _context.OEMs.Remove(oem);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> OEMExists(int id)
        {
            return await _context.OEMs.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> OEMNameExists(string name, int? excludeId = null)
        {
            var query = _context.OEMs.Where(x => x.Name.ToLower() == name.ToLower());
            
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }
            
            return await query.AnyAsync();
        }

        /// <summary>
        /// Creates a new OEM if it doesn't exist, or returns the existing one if it does.
        /// This method performs the check and creation at the database level for optimal performance.
        /// Uses case-insensitive comparison for OEM name matching.
        /// </summary>
        /// <param name="oemName">The name of the OEM to create or retrieve</param>
        /// <returns>The existing or newly created OEM entity</returns>
        public async Task<OEM> CreateIfNotExists(string oemName)
        {
            if (string.IsNullOrWhiteSpace(oemName))
            {
                throw new ArgumentException("OEM name cannot be null or empty", nameof(oemName));
            }

            var trimmedName = oemName.Trim();

            // First, try to find existing OEM with case-insensitive comparison
            var existingOEM = await _context.OEMs
                .FirstOrDefaultAsync(x => x.Name.ToLower() == trimmedName.ToLower());

            if (existingOEM != null)
            {
                return existingOEM;
            }

            // OEM doesn't exist, create a new one
            var newOEM = new OEM
            {
                Name = trimmedName
            };

            _context.OEMs.Add(newOEM);
            await _context.SaveChangesAsync();

            return newOEM;
        }
    }
}
