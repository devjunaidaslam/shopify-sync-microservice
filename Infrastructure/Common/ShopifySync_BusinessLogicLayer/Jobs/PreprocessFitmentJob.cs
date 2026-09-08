using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Globalization;
using System.Linq;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using System.Collections.Generic;
using ClosedXML.Excel;
using System.Dynamic;
 using ShopifySync_DataAccessLayer.Enum;
 using ShopifySync_BusinessLogicLayer.Infrastructure.Job.Background;

namespace ShopifySync_BusinessLogicLayer.Jobs
{
    public class PreprocessFitmentJob
    {
        private readonly ILogger<PreprocessFitmentJob> _logger;
        private readonly ITypeRepository _typeRepo;
        private readonly IYearRepository _yearRepo;
        private readonly IMakeRepository _makeRepo;
        private readonly IModelRepository _modelRepo;
        private readonly ITemporaryVehicleRepository _tempVehicleRepo;
        private readonly IImportFitmentRepository _importFitmentRepo;
        private readonly IS3StorageService _s3Service;
        private readonly IFitmentBackgroundQueue _fitmentQueue;

        /// <summary>
        /// Ensures all DateTime properties in ImportFitment have Kind=UTC to prevent PostgreSQL errors
        /// </summary>
        private void EnsureImportDateTimesAreUtc(ImportFitment import)
        {
            // Handle nullable DateTime properties
            if (import.CreatedAt.HasValue && import.CreatedAt.Value.Kind != DateTimeKind.Utc)
                import.CreatedAt = DateTime.SpecifyKind(import.CreatedAt.Value, DateTimeKind.Utc);
            
            if (import.UpdatedAt.HasValue && import.UpdatedAt.Value.Kind != DateTimeKind.Utc)
                import.UpdatedAt = DateTime.SpecifyKind(import.UpdatedAt.Value, DateTimeKind.Utc);
            
            if (import.CleanedAt.HasValue && import.CleanedAt.Value.Kind != DateTimeKind.Utc)
                import.CleanedAt = DateTime.SpecifyKind(import.CleanedAt.Value, DateTimeKind.Utc);
        }

        public PreprocessFitmentJob(
            ILogger<PreprocessFitmentJob> logger,
            ITypeRepository typeRepo,
            IYearRepository yearRepo,
            IMakeRepository makeRepo,
            IModelRepository modelRepo,
            ITemporaryVehicleRepository tempVehicleRepo,
            IImportFitmentRepository importFitmentRepo,
            IS3StorageService s3Service,
            IFitmentBackgroundQueue fitmentQueue)
        {
            _logger = logger;
            _typeRepo = typeRepo;
            _yearRepo = yearRepo;
            _makeRepo = makeRepo;
            _modelRepo = modelRepo;
            _tempVehicleRepo = tempVehicleRepo;
            _importFitmentRepo = importFitmentRepo;
            _s3Service = s3Service;
            _fitmentQueue = fitmentQueue;
        }

        public async Task ExecuteAsync(long importFitmentId)
        {
            try
            {
                // Get specific import fitment
                var import = await _importFitmentRepo.GetImportFitmentDetailsById(importFitmentId);
                if (import != null && (import.Status == ImportFitmentStatuses.Imported 
                    || import.Status == ImportFitmentStatuses.Preprocessing 
                    || import.Status == ImportFitmentStatuses.PreprocessingQueued))
                {
                    _logger.LogInformation($"Processing import fitment {import.ImportFitmentId}");

                    try
                    {
                        // Use the new function to get records from either CSV or XLSX file
                        var records = await GetRecordsFromFile(import.FileLink);

                        var tempVehicles = new List<TempVehicleImports>();
                            
                            // Fetch ALL reference data upfront for maximum performance
                            _logger.LogInformation("Fetching all reference data before processing records");
                            
                            // Load all types
                            var allTypes = await _typeRepo.GetAllTypes();
                            var typeDictionary = new Dictionary<string, VehicleTypes>(StringComparer.OrdinalIgnoreCase);
                            foreach (var type in allTypes)
                            {
                                if (!string.IsNullOrEmpty(type.Name))
                                {
                                    typeDictionary[type.Name] = type;
                                }
                            }
                            _logger.LogInformation($"Loaded {allTypes.Count()} types");
                            
                            // Load all years - using the "all" parameter to get all records
                            var allYears = await _yearRepo.GetAllVehicleYears("", 1, "all");
                            var yearDictionary = new Dictionary<string, VehicleYears>();
                            foreach (var year in allYears)
                            {
                                yearDictionary[year.Name] = year;
                            }
                            _logger.LogInformation($"Loaded {allYears.Count()} years");
                            
                            // Load all makes - using the "all" parameter to get all records
                            var allMakes = await _makeRepo.GetAllVehicleMakes("", 1, "all");
                            var makeDictionary = new Dictionary<string, VehicleMakes>(StringComparer.OrdinalIgnoreCase);
                            foreach (var make in allMakes)
                            {
                                if (!string.IsNullOrEmpty(make.Name))
                                {
                                    makeDictionary[make.Name] = make;
                                }
                            }
                            _logger.LogInformation($"Loaded {allMakes.Count()} makes");
                            
                            // Load all models - using the "all" parameter to get all records
                            var allModels = await _modelRepo.GetAllVehicleModels("", 1, "all");
                            var modelDictionary = new Dictionary<string, VehicleModels>(StringComparer.OrdinalIgnoreCase);
                            foreach (var model in allModels)
                            {
                                if (!string.IsNullOrEmpty(model.Name))
                                {
                                    modelDictionary[model.Name] = model;
                                }
                            }
                            _logger.LogInformation($"Loaded {allModels.Count()} models");
                            
                            _logger.LogInformation("Completed loading all reference data");
                            
                            foreach (var record in records)
                            {
                                var tempImport = new TempVehicleImports
                                {
                                    ImportFitmentId = import.ImportFitmentId,
                                    type = record.Category?.ToString(),                      // Assuming "Category" maps to type
                                    year = int.TryParse(record.Year?.ToString() ?? "0", out int yearVal) ? yearVal : 0,
                                    make = record.Make?.ToString(),
                                    model = record.Model?.ToString(),
                                    oem = record.Oem?.ToString(),                            // Extract OEM from record
                                    created_at = DateTime.UtcNow
                                };

                                // Process each piece of information separately without hierarchical dependencies

                                // Process Type
                                if (!string.IsNullOrEmpty(tempImport.type))
                                {
                                    if (typeDictionary.TryGetValue(tempImport.type, out var typeMatch))
                                    {
                                        tempImport.type_id = typeMatch.VehicleTypesId;
                                    }
                                    else if (import.IsDefault == true)
                                    {
                                        tempImport.is_default_type = true;
                                    }
                                }

                                // Process Year - independently of type
                                if (tempImport.year > 0)
                                {
                                    string yearStr = tempImport.year.ToString();
                                    if (yearDictionary.TryGetValue(yearStr, out var yearMatch))
                                    {
                                        tempImport.year_id = yearMatch.VehicleYearId;
                                    }
                                    else if (import.IsDefault == true)
                                    {
                                        tempImport.is_default_year = true;
                                    }
                                }

                                // Process Make - independently of type and year
                                if (!string.IsNullOrEmpty(tempImport.make))
                                {
                                    if (makeDictionary.TryGetValue(tempImport.make, out var makeMatch))
                                    {
                                        tempImport.make_id = makeMatch.VehicleMakeId;
                                    }
                                    else if (import.IsDefault == true)
                                    {
                                        tempImport.is_default_make = true;
                                    }
                                }

                                // Process Model - independently of type, year, and make
                                if (!string.IsNullOrEmpty(tempImport.model))
                                {
                                    if (modelDictionary.TryGetValue(tempImport.model, out var modelMatch))
                                    {
                                        tempImport.model_id = modelMatch.VehicleModelId;
                                    }
                                    else if (import.IsDefault == true)
                                    {
                                        tempImport.is_default_model = true;
                                    }
                                }

                                tempVehicles.Add(tempImport);
                            }

                            // Save all processed temp vehicles to database
                            _logger.LogInformation($"Saving {tempVehicles.Count} temporary vehicles to database");
                            await _tempVehicleRepo.BulkInsertTempVehicles(tempVehicles);
                            _logger.LogInformation("Temporary vehicles saved successfully");

                        // Update import status
                        import.Status = ImportFitmentStatuses.Preprocessed;
                        var processedImports = await _tempVehicleRepo.GetTempVehicleList(1, "all");
                        import.ProcessedLines = processedImports.Count();
                        import.UpdatedAt = DateTime.UtcNow;
                        
                        // Ensure DateTime fields are UTC before saving
                        EnsureImportDateTimesAreUtc(import);
                        
                        await _importFitmentRepo.UpdateImportFitment(import);

                        // If import is marked as default, automatically queue finalization
                        if (import.IsDefault == true)
                        {
                            _logger.LogInformation($"Import {import.ImportFitmentId} is default; queuing Finalize job");
                            import.Status = ImportFitmentStatuses.FinalizingQueued;
                            import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                            EnsureImportDateTimesAreUtc(import);
                            await _importFitmentRepo.UpdateImportFitment(import);

                            _fitmentQueue.Enqueue(new FitmentJobRequest(import.ImportFitmentId, FitmentJobType.Finalize));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing import fitment {import.ImportFitmentId}");
                        import.Status = ImportFitmentStatuses.Error;
                        import.UpdatedAt = DateTime.UtcNow;
                        await _importFitmentRepo.UpdateImportFitment(import);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PreprocessFitmentJob");
            }
        }

        /// <summary>
        /// Gets records from a file which can be either CSV or XLSX format
        /// </summary>
        /// <param name="fileLink">S3 file link</param>
        /// <returns>A collection of dynamic records</returns>
        /// <exception cref="NotSupportedException">Thrown when file format is not supported</exception>
        private async Task<IEnumerable<dynamic>> GetRecordsFromFile(string fileLink)
        {
            var fileStream = await _s3Service.GetFileFromS3Bucket(fileLink);
            var fileExtension = Path.GetExtension(fileLink).ToLower();
            
            _logger.LogInformation($"Processing file with extension: {fileExtension}");
            
            switch (fileExtension)
            {
                case ".csv":
                    return GetRecordsFromCsv(fileStream);
                
                case ".xlsx":
                case ".xls":
                    return GetRecordsFromExcel(fileStream);
                
                default:
                    throw new NotSupportedException($"File format {fileExtension} is not supported. Only CSV and XLSX/XLS are supported.");
            }
        }
        
        /// <summary>
        /// Extracts records from a CSV file
        /// </summary>
        /// <param name="stream">CSV file stream</param>
        /// <returns>A collection of dynamic records</returns>
        private IEnumerable<dynamic> GetRecordsFromCsv(Stream stream)
        {
            _logger.LogInformation("Processing CSV file");
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<dynamic>().ToList();
            }
        }
        
        /// <summary>
        /// Extracts records from an Excel file using ClosedXML
        /// </summary>
        /// <param name="stream">Excel file stream</param>
        /// <returns>A collection of dynamic records</returns>
        private IEnumerable<dynamic> GetRecordsFromExcel(Stream stream)
        {
            _logger.LogInformation("Processing Excel file");
            var results = new List<dynamic>();
            
            using (var workbook = new XLWorkbook(stream))
            {
                // Assuming data is in the first worksheet
                var worksheet = workbook.Worksheet(1);
                
                // Get header row
                var headerRow = worksheet.FirstRowUsed();
                var headers = new List<string>();
                
                foreach (var cell in headerRow.CellsUsed())
                {
                    headers.Add(cell.Value.ToString());
                }
                
                // Process data rows (skip header)
                var dataRows = worksheet.RowsUsed().Skip(1);
                
                foreach (var row in dataRows)
                {
                    var record = new ExpandoObject() as IDictionary<string, object>;
                    
                    for (int i = 0; i < headers.Count; i++)
                    {
                        var cell = row.Cell(i + 1); // Excel is 1-indexed
                        if (!cell.IsEmpty())
                        {
                            record[headers[i]] = cell.Value.ToString();
                        }
                        else
                        {
                            record[headers[i]] = null;
                        }
                    }
                    
                    results.Add(record as dynamic);
                }
            }
            
            return results;
        }
    }
}
