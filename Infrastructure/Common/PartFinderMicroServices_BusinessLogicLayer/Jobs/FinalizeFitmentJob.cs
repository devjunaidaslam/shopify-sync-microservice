using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
 using PartFinderMicroServices_DataAccessLayer.Enum;

namespace PartFinderMicroServices_BusinessLogicLayer.Jobs
{
    public class FinalizeFitmentJob
    {
        private readonly ILogger<FinalizeFitmentJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FinalizeFitmentJob(
            ILogger<FinalizeFitmentJob> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(long importFitmentId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var importFitmentRepo = scope.ServiceProvider.GetRequiredService<IImportFitmentRepository>();
                var tempVehicleRepo = scope.ServiceProvider.GetRequiredService<ITemporaryVehicleRepository>();
                var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
                var oemRepo = scope.ServiceProvider.GetRequiredService<IOEMRepository>();
                var oemVehicleRepo = scope.ServiceProvider.GetRequiredService<IOemVehicleRepository>();
                var typeRepo = scope.ServiceProvider.GetRequiredService<ITypeRepository>();
                var yearRepo = scope.ServiceProvider.GetRequiredService<IYearRepository>();
                var makeRepo = scope.ServiceProvider.GetRequiredService<IMakeRepository>();
                var modelRepo = scope.ServiceProvider.GetRequiredService<IModelRepository>();

                try
                {
                    // Get specific import fitment
                    var import = await importFitmentRepo.GetImportFitmentDetailsById(importFitmentId);
                    if (import != null && (import.Status == ImportFitmentStatuses.Preprocessed 
                        || import.Status == ImportFitmentStatuses.Finalizing 
                        || import.Status == ImportFitmentStatuses.FinalizingQueued))
                    {
                        _logger.LogInformation($"Finalizing import fitment {import.ImportFitmentId}");

                        try
                        {
                            // Get all temp vehicles for this import using the filter model
                            var filterModel = new TempVehicleFilterModel
                            {
                                ImportFitmentId = importFitmentId,
                                per_page = "all"
                            };
                            var tempImports = (await tempVehicleRepo.GetTempVehicleList(filterModel))
                                .Where(x => x.is_processed != true)
                                .ToList();

                            _logger.LogInformation($"Processing {tempImports.Count} temp vehicle records for import {importFitmentId}");

                            // Process each temp vehicle record
                            foreach (var tempImport in tempImports)
                            {
                                await ProcessTempVehicleRecord(
                                    tempImport,
                                    import,
                                    typeRepo,
                                    yearRepo,
                                    makeRepo,
                                    modelRepo,
                                    vehicleRepo,
                                    tempVehicleRepo,
                                    oemRepo,
                                    oemVehicleRepo);
                            }

                            // Check if all temp vehicles are processed and update import status
                            await UpdateImportStatusIfComplete(importFitmentId, import, tempVehicleRepo, importFitmentRepo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error finalizing import fitment {import.ImportFitmentId}");
                            import.Status = ImportFitmentStatuses.Error;
                            import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                            await importFitmentRepo.UpdateImportFitment(import);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Import fitment {importFitmentId} not found or not in Preprocessed status");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in FinalizeFitmentJob");
                }
            }
        }

        private async Task ProcessTempVehicleRecord(
            TempVehicleImports tempImport,
            ImportFitment import,
            ITypeRepository typeRepo,
            IYearRepository yearRepo,
            IMakeRepository makeRepo,
            IModelRepository modelRepo,
            IVehicleRepository vehicleRepo,
            ITemporaryVehicleRepository tempVehicleRepo,
            IOEMRepository oemRepo,
            IOemVehicleRepository oemVehicleRepo)
        {
            try
            {
                // Process OEM - create new OEM if it doesn't exist and OEM value is provided
                OEM? resolvedOem = null;
                if (!string.IsNullOrEmpty(tempImport.oem))
                {
                    resolvedOem = await CreateOEMIfNotExists(tempImport, oemRepo);
                }

                // Process default types - create new types if marked as default and no type_id exists
                if (!tempImport.type_id.HasValue && tempImport.is_default_type == true && !string.IsNullOrEmpty(tempImport.type))
                {
                    await CreateVehicleTypeIfNotExists(tempImport, import, typeRepo, tempVehicleRepo);
                }

                // Process default years - create new years if marked as default and no year_id exists
                if (!tempImport.year_id.HasValue && tempImport.is_default_year == true && tempImport.year > 0)
                {
                    await CreateVehicleYearIfNotExists(tempImport, import, yearRepo, tempVehicleRepo);
                }

                // Process default makes - create new makes if marked as default and no make_id exists
                if (!tempImport.make_id.HasValue && tempImport.is_default_make == true && !string.IsNullOrEmpty(tempImport.make))
                {
                    await CreateVehicleMakeIfNotExists(tempImport, import, makeRepo, tempVehicleRepo);
                }

                // Process default models - create new models if marked as default and no model_id exists
                if (!tempImport.model_id.HasValue && tempImport.is_default_model == true && !string.IsNullOrEmpty(tempImport.model))
                {
                    await CreateVehicleModelIfNotExists(tempImport, import, modelRepo, tempVehicleRepo);
                }

                // Create vehicle if all resource IDs are available
                if (tempImport.type_id.HasValue && tempImport.year_id.HasValue &&
                    tempImport.make_id.HasValue && tempImport.model_id.HasValue)
                {
                    var vehicle = await CreateVehicleIfNotExists(tempImport, vehicleRepo);
                    
                    // Ensure OEM-Vehicle relation using already resolved data
                    if (vehicle != null && resolvedOem != null)
                    {
                        await EnsureOemVehicleLink(resolvedOem.Id, vehicle.VehicleId, import.SupplierId, oemVehicleRepo);
                    }
                    
                    // Mark temp import as processed
                    tempImport.is_processed = true;
                    tempImport.updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                    // Ensure created_at has UTC kind to avoid PostgreSQL DateTime Kind issues
                    if (tempImport.created_at.Kind != DateTimeKind.Utc)
                    {
                        tempImport.created_at = DateTime.SpecifyKind(tempImport.created_at, DateTimeKind.Utc);
                    }
                    await tempVehicleRepo.UpdateTempVehicle(tempImport);
                    
                    _logger.LogDebug($"Processed temp vehicle record {tempImport.TempVehicleImportId}");
                }
                else
                {
                    _logger.LogDebug($"Temp vehicle record {tempImport.TempVehicleImportId} missing required resource IDs - Type: {tempImport.type_id}, Year: {tempImport.year_id}, Make: {tempImport.make_id}, Model: {tempImport.model_id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing temp vehicle record {tempImport.TempVehicleImportId}");
            }
        }

        private async Task CreateVehicleTypeIfNotExists(TempVehicleImports tempImport, ImportFitment import, ITypeRepository typeRepo, ITemporaryVehicleRepository tempVehicleRepo)
        {
            try
            {
                var candidate = new VehicleTypes
                {
                    Name = tempImport.type?.Trim(),
                    SupplierId = import.SupplierId,
                    ReferenceId = null
                };

                var result = await typeRepo.CreateIfNotExists(candidate);

                tempImport.type_id = result.VehicleTypesId;
                tempImport.updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                // Ensure created_at has UTC kind to avoid PostgreSQL DateTime Kind issues
                if (tempImport.created_at.Kind != DateTimeKind.Utc)
                {
                    tempImport.created_at = DateTime.SpecifyKind(tempImport.created_at, DateTimeKind.Utc);
                }
                await tempVehicleRepo.UpdateTempVehicle(tempImport);
                _logger.LogDebug($"Ensured vehicle type '{tempImport.type}' exists with ID {result.VehicleTypesId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vehicle type '{tempImport.type}'");
            }
        }

        private async Task CreateVehicleYearIfNotExists(TempVehicleImports tempImport, ImportFitment import, IYearRepository yearRepo, ITemporaryVehicleRepository tempVehicleRepo)
        {
            try
            {
                var candidate = new VehicleYears
                {
                    Name = tempImport.year.ToString(),
                    SupplierId = import.SupplierId,
                    ReferenceId = null
                };

                var result = await yearRepo.CreateIfNotExists(candidate);

                tempImport.year_id = result.VehicleYearId;
                tempImport.updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                if (tempImport.created_at.Kind != DateTimeKind.Utc)
                {
                    tempImport.created_at = DateTime.SpecifyKind(tempImport.created_at, DateTimeKind.Utc);
                }
                await tempVehicleRepo.UpdateTempVehicle(tempImport);
                _logger.LogDebug($"Ensured vehicle year '{tempImport.year}' exists with ID {result.VehicleYearId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vehicle year '{tempImport.year}'");
            }
        }

        private async Task CreateVehicleMakeIfNotExists(TempVehicleImports tempImport, ImportFitment import, IMakeRepository makeRepo, ITemporaryVehicleRepository tempVehicleRepo)
        {
            try
            {
                var candidate = new VehicleMakes
                {
                    Name = tempImport.make?.Trim(),
                    SupplierId = import.SupplierId,
                    ReferenceId = null
                };

                var result = await makeRepo.CreateIfNotExists(candidate);

                tempImport.make_id = result.VehicleMakeId;
                tempImport.updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                if (tempImport.created_at.Kind != DateTimeKind.Utc)
                {
                    tempImport.created_at = DateTime.SpecifyKind(tempImport.created_at, DateTimeKind.Utc);
                }
                await tempVehicleRepo.UpdateTempVehicle(tempImport);
                _logger.LogDebug($"Ensured vehicle make '{tempImport.make}' exists with ID {result.VehicleMakeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vehicle make '{tempImport.make}'");
            }
        }

        private async Task CreateVehicleModelIfNotExists(TempVehicleImports tempImport, ImportFitment import, IModelRepository modelRepo, ITemporaryVehicleRepository tempVehicleRepo)
        {
            try
            {
                var candidate = new VehicleModels
                {
                    Name = tempImport.model?.Trim(),
                    SupplierId = import.SupplierId,
                    ReferenceId = null
                };

                var result = await modelRepo.CreateIfNotExists(candidate);

                tempImport.model_id = result.VehicleModelId;
                tempImport.updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                if (tempImport.created_at.Kind != DateTimeKind.Utc)
                {
                    tempImport.created_at = DateTime.SpecifyKind(tempImport.created_at, DateTimeKind.Utc);
                }
                await tempVehicleRepo.UpdateTempVehicle(tempImport);
                _logger.LogDebug($"Ensured vehicle model '{tempImport.model}' exists with ID {result.VehicleModelId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vehicle model '{tempImport.model}'");
            }
        }

        private async Task<Vehicles?> CreateVehicleIfNotExists(TempVehicleImports tempImport, IVehicleRepository vehicleRepo)
        {
            try
            {
                // Check if vehicle already exists
                var existingVehicle = await vehicleRepo.GetVehicle(
                    tempImport.type_id.Value,
                    tempImport.make_id.Value,
                    tempImport.year_id.Value,
                    tempImport.model_id.Value);

                if (existingVehicle == null)
                {
                    var newVehicle = new Vehicles
                    {
                        TypeId = tempImport.type_id.Value,
                        MakeId = tempImport.make_id.Value,
                        YearId = tempImport.year_id.Value,
                        ModelId = tempImport.model_id.Value,
                        IsActive = true,
                        LastUpdate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    };
                    await vehicleRepo.CreateNewVehicle(newVehicle);
                    _logger.LogDebug($"Created new vehicle with Type: {tempImport.type_id}, Make: {tempImport.make_id}, Year: {tempImport.year_id}, Model: {tempImport.model_id}");
                    return newVehicle;
                }
                else
                {
                    _logger.LogDebug($"Vehicle already exists with Type: {tempImport.type_id}, Make: {tempImport.make_id}, Year: {tempImport.year_id}, Model: {tempImport.model_id}");
                    return existingVehicle;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vehicle for temp import {tempImport.TempVehicleImportId}");
                return null;
            }
        }

        private async Task UpdateImportStatusIfComplete(
            long importFitmentId, 
            ImportFitment import, 
            ITemporaryVehicleRepository tempVehicleRepo, 
            IImportFitmentRepository importFitmentRepo)
        {
            try
            {
                // Check if all temp vehicles for this import are processed
                var filterModel = new TempVehicleFilterModel
                {
                    ImportFitmentId = importFitmentId,
                    per_page = "all"
                };
                var allImports = await tempVehicleRepo.GetTempVehicleList(filterModel);
                var unprocessedCount = allImports.Count(x => x.is_processed != true);
                
                if (unprocessedCount == 0)
                {
                    import.Status = ImportFitmentStatuses.Completed;
                    import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                    await importFitmentRepo.UpdateImportFitment(import);
                    _logger.LogInformation($"Import fitment {importFitmentId} marked as Completed");
                }
                else
                {
                    _logger.LogInformation($"Import fitment {importFitmentId} still has {unprocessedCount} unprocessed temp vehicles");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating import status for {importFitmentId}");
            }
        }

        /// <summary>
        /// Creates a new OEM record if it doesn't already exist in the database, or returns the existing one.
        /// This method uses the repository's CreateIfNotExists method for optimal performance with large datasets.
        /// The database-level operation avoids fetching all OEMs and performs efficient case-insensitive matching.
        /// </summary>
        /// <param name="tempImport">The temporary vehicle import containing the OEM name</param>
        /// <param name="oemRepo">The OEM repository for database operations</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task<OEM?> CreateOEMIfNotExists(TempVehicleImports tempImport, IOEMRepository oemRepo)
        {
            try
            {
                // Use the efficient repository method that handles check and creation at database level
                var oem = await oemRepo.CreateIfNotExists(tempImport.oem);
                
                _logger.LogDebug($"OEM '{oem.Name}' processed with ID: {oem.Id} for temp vehicle import {tempImport.TempVehicleImportId}");
                return oem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing OEM '{tempImport.oem}' for temp vehicle import {tempImport.TempVehicleImportId}");
                return null;
            }
        }

        /// <summary>
        /// Ensures the relationship between OEM and Vehicle exists by attaching the link.
        /// Uses already-resolved OEMId and VehicleId (no additional lookups).
        /// </summary>
        private async Task EnsureOemVehicleLink(
            int? oemId,
            long vehicleId,
            long? supplierId,
            IOemVehicleRepository oemVehicleRepo)
        {
            try
            {
                if (!oemId.HasValue)
                {
                    return;
                }

                await oemVehicleRepo.AttachAsync(oemId.Value, vehicleId, supplierId);
                _logger.LogDebug($"Ensured OEM-Vehicle link: OEMId {oemId.Value} -> VehicleId {vehicleId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ensuring OEM-Vehicle link for vehicle {vehicleId}");
            }
        }
    }
}
