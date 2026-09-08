using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs;
using ShopifySync_DataAccessLayer.Entities.RabbitMQ;
using ShopifySync_DataAccessLayer.Model;
using System.Text;
using System.Text.Json;
using System.Linq;
using ShopifySync_DataAccessLayer.Entities.Vehicle;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class FitmentService : IFitmentService
    {
        private readonly ICommonService _commonService;
        private readonly IShopifyRepository _shopifyRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IShopifyUpdateService _shopifyUpdate;
        private readonly ILogger<FitmentService> _logger;
        private readonly string _token;
        private readonly string _shopUrl;
        private readonly string _version;
        private readonly ResponseMessageList _apiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="FitmentService"/> class.
        /// </summary>
        /// <param name="settings">Shopify settings options.</param>
        /// <param name="commonService">Common service for logging.</param>
        /// <param name="shopifyRepository">Shopify repository for data access.</param>
        /// <param name="vehicleRepository">Vehicle repository for data access.</param>
        /// <param name="logger">Logger for application logging.</param>
        public FitmentService(
            IOptions<ShopifySetting> settings,
            ICommonService commonService,
            IShopifyRepository shopifyRepository,
            IVehicleRepository vehicleRepository,
            IShopifyUpdateService shopifyUpdate,
            ILogger<FitmentService> logger)
        {
            _commonService = commonService;
            _shopifyRepository = shopifyRepository;
            _vehicleRepository = vehicleRepository;
            _shopifyUpdate = shopifyUpdate;
            _logger = logger;
            _token = settings.Value.Token;
            _shopUrl = $"{settings.Value.ShopName}.myshopify.com";
            _version = settings.Value.Version;
            
            _logger.LogInformation("[FitmentService] Service initialized successfully with shop URL: {ShopUrl}", _shopUrl);
        }

        /// <summary>
        /// Upserts fitment data to Shopify based on the specified mode.
        /// </summary>
        /// <param name="request">The fitment upsert request containing mode and optional variant ID or product ID.</param>
        /// <returns>A response indicating success or failure.</returns>
        public async Task<Response> UpsertFitmentDataAsync(FitmentUpsertDTO request)
        {
            _logger.LogInformation("[FitmentService] UpsertFitmentDataAsync called with mode: {Mode}, variant ID: {VariantId}, product ID: {ProductId}", 
                request.Mode, request.VariantId ?? "none", request.ProductId ?? "none");
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.Mode))
                {
                    _logger.LogWarning("[FitmentService] Mode is required but was not provided");
                    return ResponseHelper.BadRequest("Mode is required.");
                }

                if (request.Mode == "by_variant" && string.IsNullOrEmpty(request.VariantId))
                {
                    _logger.LogWarning("[FitmentService] Variant ID is required when mode is 'by_variant' but was not provided");
                    return ResponseHelper.BadRequest("Variant ID is required when mode is 'by_variant'.");
                }

                if (request.Mode == "by_product" && string.IsNullOrEmpty(request.ProductId))
                {
                    _logger.LogWarning("[FitmentService] Product ID is required when mode is 'by_product' but was not provided");
                    return ResponseHelper.BadRequest("Product ID is required when mode is 'by_product'.");
                }

                List<Variant> variantsToProcess = new List<Variant>();

                // Step 1: Select variants based on mode
                if (request.Mode == "by_variant")
                {
                    _logger.LogDebug("[FitmentService] Fetching single variant with ID: {VariantId}", request.VariantId);
                    var variant = await _shopifyRepository.GetVariantByShopifyIdAsync("gid://shopify/ProductVariant/" + request.VariantId);
                    if (variant == null)
                    {
                        _logger.LogWarning("[FitmentService] Variant with ID {VariantId} not found", request.VariantId);
                        return ResponseHelper.NotFound($"Variant with ID {request.VariantId} not found.");
                    }

                    if (variant.Product.Is_Piece && variant.Product.Exact_Fit)
                    {
                        variantsToProcess.Add(variant);
                    }
                    
                    _logger.LogDebug("[FitmentService] Found variant with ID: {VariantId}", request.VariantId);
                }
                else if (request.Mode == "by_product")
                {
                    _logger.LogDebug("[FitmentService] Fetching product with ID: {ProductId}", request.ProductId);
                    var productShopifyId = $"gid://shopify/Product/{request.ProductId}";
                    var product = await _shopifyRepository.GetProductByShopifyIdAsync(productShopifyId);
                    
                    if (product == null)
                    {
                        _logger.LogWarning("[FitmentService] Product with ID {ProductId} not found", request.ProductId);
                        return ResponseHelper.NotFound($"Product with ID {request.ProductId} not found.");
                    }

                    if (!product.Is_Piece || !product.Exact_Fit)
                    {
                        _logger.LogWarning("[FitmentService] Product with ID {ProductId} does not meet criteria (exact_fit = true and is_piece = true)", request.ProductId);
                        return ResponseHelper.BadRequest($"Product with ID {request.ProductId} does not meet criteria (exact_fit = true and is_piece = true).");
                    }

                    _logger.LogDebug("[FitmentService] Fetching variants for product ID: {ProductId}", request.ProductId);
                    variantsToProcess = await _shopifyRepository.GetVariantsByProductIdAsync(product.Id);
                    
                    _logger.LogDebug("[FitmentService] Found {Count} variants for product {ProductId}", variantsToProcess?.Count ?? 0, request.ProductId);
                }
                else if (request.Mode == "all_variants")
                {
                    _logger.LogDebug("[FitmentService] Fetching all variants");
                    variantsToProcess = await _shopifyRepository.GetAllVariantsWithProductsAsync(request.Filter);

                    _logger.LogDebug("[FitmentService] Found {Count} total variants", variantsToProcess?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("[FitmentService] Invalid mode provided: {Mode}", request.Mode);
                    return ResponseHelper.BadRequest("Invalid mode. Must be 'by_variant', 'by_product', or 'all_variants'.");
                }

                // Step 2: Filter variants (keep only variants where exact_fit = true and is_piece = true)
                _logger.LogDebug("[FitmentService] Filtering variants for exact_fit=true and is_piece=true");
                var filteredVariants = variantsToProcess;

                if (!filteredVariants.Any())
                {
                    _logger.LogWarning("[FitmentService] No variants found matching the criteria (exact_fit = true and is_piece = true)");
                    return ResponseHelper.NotFound("No variants found matching the criteria (exact_fit = true and is_piece = true).");
                }
                
                _logger.LogInformation("[FitmentService] Found {Count} variants matching criteria out of {Total} total variants", 
                    filteredVariants.Count, variantsToProcess.Count);

                int processedCount = 0;
                int successCount = 0;
                var errors = new List<string>();

                // Step 3: Process each filtered variant
                _logger.LogInformation("[FitmentService] Starting to process {Count} filtered variants", filteredVariants.Count);
                foreach (var variant in filteredVariants)
                {
                    processedCount++;
                    _logger.LogDebug("[FitmentService] Processing variant {Count}/{Total}: {ShopifyId}", 
                        processedCount, filteredVariants.Count, variant.ShopifyId);
                    try
                    {
                        // Step 4: Resolve OEM numbers
                        _logger.LogDebug("[FitmentService] Resolving OEM numbers for variant {ShopifyId}", variant.ShopifyId);
                      
                        // Step 5: Collect vehicles for those OEMs
                        _logger.LogDebug("[FitmentService] Collecting compatible vehicles for variant {ShopifyId}", variant.ShopifyId);
                        var allCompatibleVehicles = new List<VehicleFitmentData>();

                        allCompatibleVehicles = await FindVehiclesByOEMAsync(variant.OEMId.ToString());


                        // Remove duplicates based on Type, Year, Make, Model
                        _logger.LogDebug("[FitmentService] Removing duplicate vehicles for variant {ShopifyId}", variant.ShopifyId);
                        var uniqueVehicles = allCompatibleVehicles
                            .GroupBy(v => new { v.Type, v.Year, v.Make, v.Model })
                            .Select(g => g.First())
                            .ToList();

                        if (!uniqueVehicles.Any())
                        {
                            _logger.LogWarning("[FitmentService] No compatible vehicles found for variant {ShopifyId}", variant.ShopifyId);
                            errors.Add($"No compatible vehicles found for variant {variant.ShopifyId}");
                            continue;
                        }
                        
                        _logger.LogDebug("[FitmentService] Found {Count} unique compatible vehicles for variant {ShopifyId}", 
                            uniqueVehicles.Count, variant.ShopifyId);

                        // Step 6: Format the fitment string
                        _logger.LogDebug("[FitmentService] Formatting fitment data for variant {ShopifyId}", variant.ShopifyId);
                        var fitmentData = FormatFitmentData(uniqueVehicles);

                        // Step 7: Upsert the Shopify metafield
                        var variantId = variant.ShopifyId?.Replace("gid://shopify/ProductVariant/", "") ?? "";
                        _logger.LogDebug("[FitmentService] Updating Shopify metafield for variant {VariantId}", variantId);
                        var updateResult = await UpdateShopifyMetafieldAsync(variantId, fitmentData);
                        
                        if (updateResult.IsSuccess)
                        {
                            successCount++;
                            _logger.LogDebug("[FitmentService] Successfully updated variant {ShopifyId}", variant.ShopifyId);
                        }
                        else
                        {
                            _logger.LogWarning("[FitmentService] Failed to update variant {ShopifyId}: {Message}", 
                                variant.ShopifyId, updateResult.Message);
                            errors.Add($"Failed to update variant {variant.ShopifyId}: {updateResult.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FitmentService] Error processing variant {ShopifyId}: {Message}", 
                            variant.ShopifyId, ex.Message);
                        errors.Add($"Error processing variant {variant.ShopifyId}: {ex.Message}");
                        _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpsertFitmentDataAsync", 1, ex.Message, ex.ToString());
                    }
                }

                var message = $"Processed {processedCount} variants. Successfully updated {successCount} variants.";
                if (errors.Any())
                {
                    message += $" Failed: {string.Join("; ", errors.Take(5))}";
                    if (errors.Count > 5)
                    {
                        message += $" and {errors.Count - 5} more failed.";
                    }
                }

                _logger.LogInformation("[FitmentService] UpsertFitmentDataAsync completed. Processed: {Processed}, Success: {Success}, Errors: {ErrorCount}", 
                    processedCount, successCount, errors.Count);
                return ResponseHelper.Success(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FitmentService] Error occurred in UpsertFitmentDataAsync: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpsertFitmentDataAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage);
            }
        }

        /// <summary>
        /// Finds vehicles compatible with the given OEM number.
        /// </summary>
        /// <param name="oemNumber">The OEM number to search for.</param>
        /// <returns>A list of compatible vehicles.</returns>
        private async Task<List<VehicleFitmentData>> FindVehiclesByOEMAsync(string oemNumber)
        {
            try
            {
                var compatibleVehicles = new List<VehicleFitmentData>();

                // Get vehicles with matching OEM number
                // GetAllVehicleList already returns VehicleDTO with TypeName, YearName, MakeName, ModelName
                var vehicles = await _vehicleRepository.GetAllVehicleList(new VehicleModel.VehicleFilterModel() 
                { 
                    per_page = "all", 
                    search = oemNumber 
                });
                
                if (vehicles != null && vehicles.Any())
                {
                    foreach (var vehicle in vehicles)
                    {
                        // VehicleDTO already contains all the needed information
                        var fitmentData = new VehicleFitmentData
                        {
                            Type = vehicle.TypeName ?? "Unknown",
                            Year = vehicle.YearName ?? "Unknown",
                            Make = vehicle.MakeName ?? "Unknown",
                            Model = vehicle.ModelName ?? "Unknown"
                        };
                        
                        compatibleVehicles.Add(fitmentData);
                    }
                }

                return compatibleVehicles;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "FindVehiclesByOEMAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Formats the vehicle fitment data according to the required format.
        /// </summary>
        /// <param name="vehicles">The list of compatible vehicles.</param>
        /// <returns>The formatted fitment data string.</returns>
        private string FormatFitmentData(List<VehicleFitmentData> vehicles)
        {
            if (!vehicles.Any())
                return string.Empty;

            var fitmentEntries = vehicles.Select(v => $"{v.Type}|{v.Year}|{v.Make}|{v.Model}");
            return string.Join("^^", fitmentEntries);
        }

        /// <summary>
        /// Updates the Shopify metafield with the fitment data.
        /// </summary>
        /// <param name="variantId">The Shopify variant ID.</param>
        /// <param name="fitmentData">The formatted fitment data.</param>
        /// <returns>A response indicating success or failure.</returns>
        private async Task<Response> UpdateShopifyMetafieldAsync(string variantId, string fitmentData)
        {
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                var variantGuid = $"gid://shopify/ProductVariant/{variantId}";
                var variables = new
                {
                    metafields = new[]
                    {
                        new
                        {
                            ownerId = variantGuid,
                            @namespace = "global",
                            key = "ymm",
                            value = fitmentData,
                            type = "single_line_text_field"
                        }
                    }
                };

                // Get product ID for transaction logging
                var variant = await _shopifyRepository.GetVariantByShopifyIdAsync(variantGuid);
                var productId = variant?.Product?.ShopifyId;

                await _shopifyUpdate.UpdateShopifyVariantMetaFieldAsync(variables);
               

                return ResponseHelper.Success("Metafield updated successfully");
            }
            catch (Exception ex)
            {
                // Get product ID for transaction logging
                var variantGuid = $"gid://shopify/ProductVariant/{variantId}";
                var variant = await _shopifyRepository.GetVariantByShopifyIdAsync(variantGuid);
                var productId = variant?.Product?.ShopifyId;


                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateShopifyMetafieldAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.BadRequest("Failed to update Shopify metafield due to an internal error.");
            }
        }
    }
}