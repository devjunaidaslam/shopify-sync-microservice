using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.Display;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList.EF;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class TemporaryVehicleRepository : ITemporaryVehicleRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public TemporaryVehicleRepository(
            IConfiguration configuration,
            PartFinderDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<TempVehicleImports>> GetTempVehicleList(TempVehicleFilterModel filterModel)
        {
            IEnumerable<TempVehicleImports> items = null;
            var query = _context.TempVehicleImports.AsQueryable();
            int perPage = DefaultPageSize;

            // Apply ImportFitmentId filter if it's specified
            if (filterModel.ImportFitmentId.HasValue && filterModel.ImportFitmentId.Value > 0)
            {
                query = query.Where(v => v.ImportFitmentId == filterModel.ImportFitmentId.Value);
            }

            // Apply HasConflicts filter if specified
            if (filterModel.HasConflicts.HasValue && filterModel.HasConflicts.Value)
            {
                query = query.Where(v => 
                    // Type conflict: no type_id and is_default_type is null or false
                    (!v.type_id.HasValue && (v.is_default_type == null || v.is_default_type == false)) ||
                    // Year conflict: no year_id and is_default_year is null or false  
                    (!v.year_id.HasValue && (v.is_default_year == null || v.is_default_year == false)) ||
                    // Make conflict: no make_id and is_default_make is null or false
                    (!v.make_id.HasValue && (v.is_default_make == null || v.is_default_make == false)) ||
                    // Model conflict: no model_id and is_default_model is null or false
                    (!v.model_id.HasValue && (v.is_default_model == null || v.is_default_model == false))
                );
            }

            if (filterModel.per_page?.ToLower() == "all")
            {
                // Return all records without pagination when "all" is specified
                items = await query
                    .ToListAsync();
            }
            else
            {
                // Use DefaultPageSize when per_page is null or couldn't be parsed
                perPage = DefaultPageSize;
                if (!string.IsNullOrEmpty(filterModel.per_page))
                {
                    int.TryParse(filterModel.per_page, out int parsedPageSize);
                    if (parsedPageSize > 0)
                    {
                        perPage = parsedPageSize;
                    }
                }
                
                int pageNo = filterModel.page_no.HasValue ? filterModel.page_no.Value : 1;
                pageNo = pageNo > 0 ? pageNo : 1;
                items = await query
                        .ToPagedListAsync(pageNo, perPage);
            }
           

            return items;
        }

        public async Task<IEnumerable<TempVehicleImports>> GetTempVehicleList(int pageNo, string pageSize)
        {
            // Forward call to the new method using the filter model
            var filterModel = new TempVehicleFilterModel
            {
                page_no = pageNo,
                per_page = pageSize
            };
            
            return await GetTempVehicleList(filterModel);
        }

        public async Task<TempVehicleImports> GetTempVehicleDetailsById(long id) =>
            await _context.TempVehicleImports.FindAsync(id);


        public async Task CreateNewTempVehicle(TempVehicleImports tempVehicleImports)
        {
            _context.TempVehicleImports.Add(tempVehicleImports);
            await _context.SaveChangesAsync();
        }

        public async Task BulkCreateTempVehicle(List<TempVehicleImports> listTempVehicle)
        {
            _context.TempVehicleImports.AddRange(listTempVehicle );
            await _context.SaveChangesAsync();
        }

        public async Task BulkInsertTempVehicles(List<TempVehicleImports> tempVehicles)
        {
            _context.TempVehicleImports.AddRange(tempVehicles);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTempVehicle(TempVehicleImports tempVehicleImports)
        {
            tempVehicleImports.updated_at = DateTime.UtcNow;
            _context.TempVehicleImports.Update(tempVehicleImports);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTempVehicle(long tempVehicleId)
        {
            var tempVehicleImports = await GetTempVehicleDetailsById(tempVehicleId);
            if (tempVehicleImports != null)
            {
                _context.TempVehicleImports.Remove(tempVehicleImports);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateNewImportFitment(ImportFitment importFitment)
        {
            _context.ImportFitment.Add(importFitment);
            await _context.SaveChangesAsync();
        }

        public async Task<int> BulkUpdateVehicleAttribute(long tempVehicleImportId, string attributeType, string attributeValue, long? newAttributeId, bool setAsDefault)
        {
            var query = _context.TempVehicleImports
                .Where(x => x.ImportFitmentId == tempVehicleImportId);

            // Apply attribute-specific filter based on attributeType
            switch (attributeType.ToLower())
            {
                case "type":
                    query = query.Where(x => x.type != null && x.type.ToLower() == attributeValue.ToLower());
                    break;
                case "make":
                    query = query.Where(x => x.make != null && x.make.ToLower() == attributeValue.ToLower());
                    break;
                case "year":
                    if (int.TryParse(attributeValue, out int yearValue))
                    {
                        query = query.Where(x => x.year == yearValue);
                    }
                    else
                    {
                        return 0; // Invalid year value
                    }
                    break;
                case "model":
                    query = query.Where(x => x.model != null && x.model.ToLower() == attributeValue.ToLower());
                    break;
                default:
                    return 0; // Invalid attribute type
            }

            var recordsToUpdate = await query.ToListAsync();

            if (recordsToUpdate.Any())
            {
                foreach (var record in recordsToUpdate)
                {
                    switch (attributeType.ToLower())
                    {
                        case "type":
                            if (newAttributeId.HasValue)
                            {
                                record.type_id = newAttributeId.Value;
                                record.is_default_type = false;
                            }
                            else if (setAsDefault)
                            {
                                record.is_default_type = true;
                                record.type_id = null;
                            }
                            break;
                        case "make":
                            if (newAttributeId.HasValue)
                            {
                                record.make_id = newAttributeId.Value;
                                record.is_default_make = false;
                            }
                            else if (setAsDefault)
                            {
                                record.is_default_make = true;
                                record.make_id = null;
                            }
                            break;
                        case "year":
                            if (newAttributeId.HasValue)
                            {
                                record.year_id = newAttributeId.Value;
                                record.is_default_year = false;
                            }
                            else if (setAsDefault)
                            {
                                record.is_default_year = true;
                                record.year_id = null;
                            }
                            break;
                        case "model":
                            if (newAttributeId.HasValue)
                            {
                                record.model_id = newAttributeId.Value;
                                record.is_default_model = false;
                            }
                            else if (setAsDefault)
                            {
                                record.is_default_model = true;
                                record.model_id = null;
                            }
                            break;
                    }
                }

                await _context.SaveChangesAsync();
            }

            return recordsToUpdate.Count;
        }

    }
}
