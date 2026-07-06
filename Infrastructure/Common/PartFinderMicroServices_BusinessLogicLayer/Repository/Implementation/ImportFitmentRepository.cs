using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ImportFitment;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList.EF;
  using PartFinderMicroServices_DataAccessLayer.Enum;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class ImportFitmentRepository : IImportFitmentRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public ImportFitmentRepository(
            IConfiguration configuration,
            PartFinderDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<ImportFitment>> GetImportFitmentList(int pageNo, string pageSize)
        {
            IEnumerable<ImportFitment> items = null;
            var query = _context.ImportFitment
                .Include(x => x.Supplier)
                .AsQueryable();


            if (pageSize != null && pageSize.ToLower() == "all")
            {
                items = await query.ToListAsync();
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
                        .ToPagedListAsync(pageNo, perPage);

            }
            return items;
        }



        public async Task<ImportFitment> GetImportFitmentDetailsById(long id) =>
            await _context.ImportFitment
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.ImportFitmentId == id);


        public async Task CreateNewImportFitment(ImportFitment importFitment)
        {
            _context.ImportFitment.Add(importFitment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateImportFitment(ImportFitment importFitment)
        {
            // Ensure UpdatedAt is UTC to satisfy PostgreSQL timestamptz requirements
            if (importFitment.UpdatedAt.HasValue && importFitment.UpdatedAt.Value.Kind != DateTimeKind.Utc)
            {
                importFitment.UpdatedAt = DateTime.SpecifyKind(importFitment.UpdatedAt.Value, DateTimeKind.Utc);
            }

            // Attach if not tracked, then mark only the intended fields as modified
            var entry = _context.Entry(importFitment);
            if (entry.State == EntityState.Detached)
            {
                _context.ImportFitment.Attach(importFitment);
                entry = _context.Entry(importFitment);
            }

            // Only update Status and UpdatedAt
            entry.Property(x => x.Status).IsModified = true;
            entry.Property(x => x.UpdatedAt).IsModified = true;

            // Explicitly avoid updating other fields (prevents Unspecified DateTime writes)
            entry.Property(x => x.CreatedAt).IsModified = false;
            entry.Property(x => x.CleanedAt).IsModified = false;
            entry.Property(x => x.FileLink).IsModified = false;
            entry.Property(x => x.ProcessedLines).IsModified = false;
            entry.Property(x => x.Category).IsModified = false;
            entry.Property(x => x.IsDefault).IsModified = false;
            entry.Property(x => x.SupplierId).IsModified = false;
            entry.Property(x => x.UserId).IsModified = false;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteImportFitment(long importFitmentId)
        {
            var importFitment = await GetImportFitmentDetailsById(importFitmentId);
            if (importFitment != null)
            {
                _context.ImportFitment.Remove(importFitment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ImportFitment>> GetPendingImports()
        {
            return await _context.ImportFitment
                .Include(x => x.Supplier)
                .Where(x => x.Status == ImportFitmentStatuses.Imported 
                         || x.Status == ImportFitmentStatuses.Preprocessing 
                         || x.Status == ImportFitmentStatuses.PreprocessingQueued)
                .ToListAsync();
        }

        public async Task<IEnumerable<ImportFitment>> GetPreprocessedImports()
        {
            return await _context.ImportFitment
                .Include(x => x.Supplier)
                .Where(x => x.Status == ImportFitmentStatuses.Preprocessed 
                         || x.Status == ImportFitmentStatuses.Finalizing 
                         || x.Status == ImportFitmentStatuses.FinalizingQueued)
                .ToListAsync();
        }
    }
}
