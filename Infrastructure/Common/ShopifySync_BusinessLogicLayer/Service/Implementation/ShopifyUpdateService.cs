using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdatePriceRequest;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class ShopifyUpdateService : IShopifyUpdateService
    {
        private readonly string _token;
        private readonly string _shopUrl;
        private readonly string _version;
        private readonly ICommonService _commonService;
        private readonly IShopifyRepository _shopifyRepo;
        private readonly ILogger<ShopifyUpdateService> _logger;
        const string metaFieldKey = "custom_prices";
        const string variPriceNameSpace = "gnt_vari_price";
        /// <summary>
        /// Initializes a new instance of the <see cref="ShopifyUpdateService"/> class.
        /// </summary>
        /// <param name="settings">Shopify settings options.</param>
        /// <param name="commonService">Common service for logging.</param>
        /// <param name="logger">Logger for application logging.</param>
        public ShopifyUpdateService(IOptions<ShopifySetting> settings, ICommonService commonService, IShopifyRepository shopifyRepo, ILogger<ShopifyUpdateService> logger)
        {
            _commonService = commonService;
            _logger = logger;
            _token = settings.Value.Token;
            _shopUrl = $"{settings.Value.ShopName}.myshopify.com";
            _version = settings.Value.Version;
            _shopifyRepo = shopifyRepo;
            
            _logger.LogInformation("[ShopifyUpdateService] Service initialized successfully with shop URL: {ShopUrl}", _shopUrl);
        }


        /// <summary>
        /// Updates the price and compare-at price for a Shopify product variant.
        /// </summary>
        /// <param name="request">The request containing variant ID and new prices.</param>
        /// <returns>A string indicating the result or error message.</returns>
        public async Task<string> UpdateVariantPricesAsync(UpdateVariantPricesRequest request)
        {
            _logger.LogInformation("[ShopifyUpdateService] UpdateVariantPricesAsync called for variant ID: {VariantId}, new price: {Price}, new compare price: {ComparePrice}", 
                request.VariantId, request.NewPrice, request.NewCompareAtPrice);
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyUpdateService] HTTP client configured for variant price update");

                var variantGid = $"gid://shopify/ProductVariant/{request.VariantId.ToString()}";
                
                var variant = await _shopifyRepo.GetVariantByShopifyIdAsync(variantGid);
                
                if (variant == null || variant.Product == null)
                {
                    var errorMessage = $"Variant with Shopify ID {variantGid} or its associated product not found in database";
                    _logger.LogError("[ShopifyUpdateService] {ErrorMessage}", errorMessage);
                    _commonService.ErrorLogs(errorMessage, "UpdateVariantPricesAsync", 1, "", "");
                    throw new Exception(errorMessage);
                }
                
                var productId = variant.Product.ShopifyId;
                _logger.LogDebug("[ShopifyUpdateService] Retrieved product ID {ProductId} for variant {VariantId}", 
                    productId, request.VariantId);
                

                var updateMutation = @"
                    mutation productVariantsBulkUpdate($productId: ID!, $variants: [ProductVariantsBulkInput!]!) {
                      productVariantsBulkUpdate(productId: $productId, variants: $variants) {
                        product {
                          id
                        }
                        productVariants {
                          id
                          price
                          compareAtPrice
                        }
                        userErrors {
                          field
                          message
                        }
                      }
                    }";

                var updateVariables = new
                {
                    productId = productId,
                    variants = new[]
                    {
                        new
                        {
                            id = variantGid,
                            price = request.NewPrice,
                            compareAtPrice = request.NewCompareAtPrice
                        }
                    }
                };

                var updateRequestBody = new
                {   
                    query = updateMutation,
                    variables = updateVariables
                };

                var updateResponse = await client.PostAsync("", new StringContent(JsonSerializer.Serialize(updateRequestBody), Encoding.UTF8, "application/json"));
                var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();

                using var updateDoc = JsonDocument.Parse(updateResponseContent);

                if (updateDoc.RootElement.TryGetProperty("errors", out var topLevelErrors))
                {
                    var errorMessages = topLevelErrors
                                .EnumerateArray()
                                .Select(err => err.GetProperty("message").GetString())
                                .ToList();

                    string combinedErrors = string.Join(" | ", errorMessages);
                    _logger.LogError("[ShopifyUpdateService] Top-level errors in UpdateVariantPricesAsync for variant {VariantId}: {Errors}", 
                        request.VariantId, combinedErrors);

                    _commonService.ErrorLogs(combinedErrors, "UpdateVariantPricesAsync", 1, "", "");
                    throw new Exception();
                }

                var userErrors = updateDoc.RootElement
                    .GetProperty("data")
                    .GetProperty("productVariantsBulkUpdate")
                    .GetProperty("userErrors");

                if (userErrors.GetArrayLength() > 0)
                {
                    var errorMessages = userErrors.EnumerateArray()
                        .Select(err => err.GetProperty("message").GetString())
                        .ToList();
                    _logger.LogError("[ShopifyUpdateService] User errors in UpdateVariantPricesAsync for variant {VariantId}: {Errors}", 
                        request.VariantId, string.Join("; ", errorMessages));
                    _commonService.ErrorLogs(string.Join("; ", errorMessages), "UpdateVariantPricesAsync", 1, "", "");
                    throw new Exception();
                }

                _logger.LogInformation("[ShopifyUpdateService] Successfully updated variant prices for variant {VariantId}", request.VariantId);
                return updateResponseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateService] Error occurred in UpdateVariantPricesAsync for variant {VariantId}: {Message}", 
                    request.VariantId, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantPricesAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Updates the price for a Shopify product variant at a specific location.
        /// </summary>
        /// <param name="request">The request containing variant ID, location ID, and new price.</param>
        /// <returns>A string indicating the result or error message.</returns>
        public async Task<string> UpdateVariantLocationPriceAsync(UpdateVariantLocationPriceRequest request)
        {
            _logger.LogInformation("[ShopifyUpdateService] UpdateVariantLocationPriceAsync called for variant ID: {VariantId}, location count: {LocationCount}", 
                request.VariantId, request.LocationPricePairs?.Count ?? 0);
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyUpdateService] HTTP client configured for variant location price update");

                var updatedValueJson = string.Empty;
                var variantGuid = $"gid://shopify/ProductVariant/{request.VariantId.ToString()}";

                JsonDocument jsonDoc = await QueryVariantWithNameSpaceAsync(client, variantGuid, variPriceNameSpace);

                jsonDoc.RootElement.TryGetProperty("data", out var dataElement);
                dataElement.TryGetProperty("productVariant", out var productVariantElement);

                if (productVariantElement.ValueKind == JsonValueKind.Null)
                {
                    string errorMessage = $"Variant with ID {variantGuid} does not exist in Shopify.";
                    _logger.LogError("[ShopifyUpdateService] Variant not found in Shopify: {VariantGuid}", variantGuid);
                    _commonService.ErrorLogs(errorMessage, "UpdateVariantLocationPriceAsync", 1, errorMessage, errorMessage);
                    throw new Exception();
                }

                var metafieldNode = productVariantElement
                                    .GetProperty("metafields").GetProperty("edges")
                                    .EnumerateArray()
                                    .FirstOrDefault(edge => edge.GetProperty("node").GetProperty("key").GetString() == metaFieldKey);

                var locationShopifyIds = request.LocationPricePairs.Select(x => x.LocationId).ToList();

                if (metafieldNode.ValueKind == JsonValueKind.Undefined)
                {
                    var prices = new Dictionary<string, string>();

                    foreach (var pair in request.LocationPricePairs)
                    {
                        prices[pair.LocationId] = pair.NewPrice.ToString("0.00");
                    }

                    updatedValueJson = JsonSerializer.Serialize(prices);
                }
                else
                {

                    var currentValueJson = metafieldNode.GetProperty("node").GetProperty("value").GetString();

                    var prices = JsonSerializer.Deserialize<Dictionary<string, string>>(currentValueJson);

                    var shopifyLocationIds = prices.Keys.ToList();

                    var existinglocationToUpdate = request.LocationPricePairs.Where(x => shopifyLocationIds.Contains(x.LocationId)).Select(x => new { x.LocationId, x.NewPrice }).ToList();

                    var newLocationToAdd = request.LocationPricePairs.Where(x => !(shopifyLocationIds.Contains(x.LocationId))).Select(x => new { x.LocationId, x.NewPrice }).ToList();

                    foreach (var pair in existinglocationToUpdate)
                    {
                        prices[pair.LocationId] = pair.NewPrice.ToString("0.00");
                    }

                    foreach (var pair in newLocationToAdd)
                    {
                        prices[pair.LocationId] = pair.NewPrice.ToString("0.00");
                    }

                    updatedValueJson = JsonSerializer.Serialize(prices);
                }


               var variables = new
                {
                    metafields = new[]
                    {
                    new
                        {
                            ownerId = variantGuid,
                            @namespace = variPriceNameSpace,
                            key = metaFieldKey,
                            value = updatedValueJson,
                            type = "json"
                        }
                    }
                };
               
                string updateJson = await UpdateShopifyVariantMetaFieldAsync(variables);

                var updateDoc = JsonDocument.Parse(updateJson);

                var userErrors = updateDoc.RootElement
                    .GetProperty("data")
                    .GetProperty("metafieldsSet")
                    .GetProperty("userErrors");

                if (userErrors.GetArrayLength() == 0)
                {
                    try
                    {
                        var variant = await _shopifyRepo.GetVariantByShopifyIdAsync(variantGuid);
                        if (variant == null)
                        {
                            _logger.LogWarning("[ShopifyUpdateService] Variant not found in database: {VariantGuid}", variantGuid);
                            return updateJson; // Return success from Shopify but log warning
                        }

                        var dbLocations = await _shopifyRepo.GetLocationsByShopifyIdsAsync(locationShopifyIds);

                        if (dbLocations == null || !dbLocations.Any())
                        {
                            _logger.LogWarning("[ShopifyUpdateService] No locations found in database for the provided location IDs");
                            return updateJson;
                        }

                        var existingVariantPrices = await _shopifyRepo.GetVariantPricesByVariantIdAsync(variant.Id);

                        _logger.LogInformation("[ShopifyUpdateService] Found {Count} existing variant prices for Variant {VariantId}",
                            existingVariantPrices?.Count ?? 0, variant.Id);

                        var variantPricesToUpdate = new List<VariantPrice>();
                        var variantPricesToAdd = new List<VariantPrice>();


                        foreach (var pair in request.LocationPricePairs)
                        {
                            var dbLocation = dbLocations.FirstOrDefault(x => x.ShopifyId == pair.LocationId);

                            if (dbLocation == null)
                            {
                                _logger.LogWarning("[ShopifyUpdateService] Location not found in database: {LocationId}", pair.LocationId);
                                continue; // Skip this location
                            }

                            _logger.LogDebug("[ShopifyUpdateService] Processing price update for Variant {VariantId} (DB ID: {DbVariantId}), Location {LocationId} (DB ID: {DbLocationId})",
                                request.VariantId, variant.Id, pair.LocationId, dbLocation.Id);

                    
                            var matchingVariantPrices = existingVariantPrices?
                                .Where(vp => vp.VariantId == variant.Id && vp.LocationId == dbLocation.Id)
                                .ToList();

                            if (matchingVariantPrices != null && matchingVariantPrices.Any())
                            {
                                _logger.LogWarning("[ShopifyUpdateService] Found {Count} existing price records for Variant {VariantId}, Location {LocationId}",
                                    matchingVariantPrices.Count, variant.Id, dbLocation.Id);

                                var priceToUpdate = matchingVariantPrices.First();
                                priceToUpdate.Price = pair.NewPrice.ToString("0.00");
                                priceToUpdate.UpdatedAt = DateTime.UtcNow;
                                variantPricesToUpdate.Add(priceToUpdate);

                                _logger.LogDebug("[ShopifyUpdateService] Updating variant price (ID: {PriceId}) for Variant {VariantId}, Location {LocationId}",
                                    priceToUpdate.Id, variant.Id, dbLocation.Id);

                            }
                            else
                            {

                                var newVariantPrice = new VariantPrice()
                                {
                                    VariantId = variant.Id,
                                    Variant = variant, 
                                    LocationId = dbLocation.Id,
                                    Location = dbLocation,
                                    Price = pair.NewPrice.ToString("0.00")
                                };
                                variantPricesToAdd.Add(newVariantPrice);
                                _logger.LogDebug("[ShopifyUpdateService] Creating new variant price for Variant {VariantId}, Location {LocationId}",
                                    variant.Id, dbLocation.Id);
                            }
                        }

                        if (variantPricesToUpdate.Any())
                        {
                            await _shopifyRepo.UpdateVariantPriceAsync(variantPricesToUpdate);
                            _logger.LogInformation("[ShopifyUpdateService] Updated {Count} existing variant prices", variantPricesToUpdate.Count);
                        }

                        if (variantPricesToAdd.Any())
                        {
                            await _shopifyRepo.AddVariantPriceAsync(variantPricesToAdd);
                            _logger.LogInformation("[ShopifyUpdateService] Added {Count} new variant prices", variantPricesToAdd.Count);
                        }

                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "[ShopifyUpdateService] Error updating database for variant {VariantGuid}: {Message}",
                            variantGuid, dbEx.Message);
                        // Don't throw - Shopify update was successful, database sync failed
                        // You might want to queue this for retry later
                    }
                }
                else
                {
                    throw new Exception();
                }
                    _logger.LogInformation("[ShopifyUpdateService] Successfully updated variant location prices for variant {VariantId}", request.VariantId);
                return updateJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateService] Error occurred in UpdateVariantLocationPriceAsync for variant {VariantId}: {Message}", 
                    request.VariantId, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantLocationPriceAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        public async Task<string> UpdateShopifyVariantMetaFieldAsync(object payload)
        {
            _logger.LogDebug("[ShopifyUpdateService] UpdateShopifyVariantMetaFieldAsync called");
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyUpdateService] HTTP client configured for metafield update");

                var updateMutation = @"
                                mutation metafieldsSet($metafields: [MetafieldsSetInput!]!) {
                                  metafieldsSet(metafields: $metafields) {
                                    metafields {
                                      id
                                      key
                                      value
                                      type
                                    }
                                    userErrors {
                                      field
                                      message
                                    }
                                  }
                                }";

                var updatePayload = new
                {
                    query = updateMutation,
                    variables = payload
                };

                var updateResponse = await client.PostAsync("", new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json"));
                var responseJson = await updateResponse.Content.ReadAsStringAsync();
                var updateDoc = JsonDocument.Parse(responseJson);

                if (updateDoc.RootElement.TryGetProperty("errors", out var topLevelErrors))
                {
                    var errorMessages = topLevelErrors
                               .EnumerateArray()
                               .Select(err => err.GetProperty("message").GetString())
                               .ToList();

                    string combinedErrors = string.Join(" | ", errorMessages);
                    _logger.LogError("[ShopifyUpdateService] Top-level errors in UpdateShopifyVariantMetaFieldAsync: {Errors}", combinedErrors);

                    _commonService.ErrorLogs(combinedErrors, "UpdateShopifyVariantMetaFieldAsync", 1, "", "");
                    return $"Top-level error: {combinedErrors}";
                }

                var userErrors = updateDoc.RootElement
                    .GetProperty("data")
                    .GetProperty("metafieldsSet")
                    .GetProperty("userErrors");

                if (userErrors.GetArrayLength() > 0)
                {
                    var errorMessages = userErrors.EnumerateArray()
                        .Select(err => err.GetProperty("message").GetString())
                        .ToList();
                    _logger.LogError("[ShopifyUpdateService] User errors in UpdateShopifyVariantMetaFieldAsync: {Errors}", string.Join("; ", errorMessages));
                    _commonService.ErrorLogs(string.Join("; ", errorMessages), "UpdateShopifyVariantMetaFieldAsync", 1, "", "");
                    return $"Shopify user errors: {string.Join("; ", errorMessages)}";
                }

                _logger.LogDebug("[ShopifyUpdateService] Successfully updated Shopify variant metafield");
                var updateJson = await updateResponse.Content.ReadAsStringAsync();
                return updateJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateService] Error occurred in UpdateShopifyVariantMetaFieldAsync: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateShopifyVariantMetaFieldAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<JsonDocument> QueryVariantWithNameSpaceAsync(HttpClient client, string variantGuid, string nameSpace)
        {
            _logger.LogDebug("[ShopifyUpdateService] QueryVariantWithNameSpaceAsync called for variant: {VariantGuid}, namespace: {NameSpace}", variantGuid, nameSpace);
            try
            {
                var getQuery = $@"
                       query {{
                         productVariant(id: ""{variantGuid}"") {{
                           metafields(first: 10, namespace: ""{nameSpace}"") {{
                             edges {{
                               node {{
                                 id
                                 key
                                 value
                                 type
                               }}
                             }}
                           }}
                         }}
                       }}";

                var getPayload = new { query = getQuery };
                var getResponse = await client.PostAsync(
         "", new StringContent(JsonSerializer.Serialize(getPayload), Encoding.UTF8, "application/json"));

                var getJson = await getResponse.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(getJson);
                _logger.LogDebug("[ShopifyUpdateService] Successfully queried variant {VariantGuid} with namespace {NameSpace}", variantGuid, nameSpace);
                return jsonDoc;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateService] Error occurred in QueryVariantWithNameSpaceAsync for variant {VariantGuid}: {Message}", variantGuid, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "QueryVariantWithNameSpaceAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Updates inventory levels for multiple variants across different locations in Shopify.
        /// </summary>
        /// <param name="request">The request containing inventory level update information.</param>
        /// <returns>JSON response from Shopify API or error message.</returns>
        public async Task<string> UpdateInventoryLevelsAsync(UpdateInventoryRequest request)
        {
            _logger.LogInformation("[ShopifyUpdateService] UpdateInventoryLevelsAsync called with {Count} inventory items", request.InventoryItems.Count);
            
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyUpdateService] HTTP client configured for inventory level updates");

                var results = new List<object>();
                var errors = new List<string>();

                // Process each inventory item individually for better error handling
                foreach (var item in request.InventoryItems)
                {
                    try
                    {
                        _logger.LogDebug("[ShopifyUpdateService] Processing inventory update for VariantId: {VariantId}, LocationId: {LocationId}, Quantity: {Quantity}", 
                            item.VariantId, item.LocationId, item.AvailableQuantity);

                        var inventoryItemId = await GetInventoryItemIdFromVariantAsync(client, item.VariantId);
                        if (string.IsNullOrEmpty(inventoryItemId))
                        {
                            var error = $"Could not find inventory item for variant {item.VariantId}";
                            _logger.LogWarning("[ShopifyUpdateService] {Error}", error);
                            errors.Add(error);
                            continue;
                        }

                        var mutation = @"
                            mutation inventorySetOnHandQuantities($input: InventorySetOnHandQuantitiesInput!) {
                              inventorySetOnHandQuantities(input: $input) {
                                inventoryAdjustmentGroup {
                                  id
                                  reason
                                  changes {
                                    name
                                    delta
                                    item {
                                      id
                                      sku
                                    }
                                    location {
                                      id
                                      name
                                    }
                                  }
                                }
                                userErrors {
                                  field
                                  message
                                }
                              }
                            }";

                        var variables = new
                        {
                            input = new
                            {
                                reason = "correction",
                                setQuantities = new[]
                                {
                                    new
                                    {
                                        inventoryItemId = inventoryItemId,
                                        locationId = $"gid://shopify/Location/{item.LocationId}",
                                        quantity = item.AvailableQuantity
                                    }
                                }
                            }
                        };

                        var requestBody = new
                        {
                            query = mutation,
                            variables
                        };

                        _logger.LogDebug("[ShopifyUpdateService] Sending inventory update request to Shopify for variant {VariantId}", item.VariantId);
                        var result = await client.PostAsync("", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                        var responseContent = await result.Content.ReadAsStringAsync();

                        using var jsonDoc = JsonDocument.Parse(responseContent);

                        if (jsonDoc.RootElement.TryGetProperty("errors", out var topLevelErrors))
                        {
                            var errorMessages = topLevelErrors
                                .EnumerateArray()
                                .Select(err => err.GetProperty("message").GetString())
                                .ToList();
                            var combinedErrors = string.Join(" | ", errorMessages);
                            _logger.LogError("[ShopifyUpdateService] Top-level errors for variant {VariantId}: {Errors}", item.VariantId, combinedErrors);
                            errors.Add($"Variant {item.VariantId}: {combinedErrors}");
                            continue;
                        }

                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("inventorySetOnHandQuantities", out var setQuantities))
                        {
                            if (setQuantities.TryGetProperty("userErrors", out var userErrors) &&
                                userErrors.GetArrayLength() > 0)
                            {
                                var userErrorMessages = userErrors
                                    .EnumerateArray()
                                    .Select(err => err.GetProperty("message").GetString())
                                    .ToList();
                                var combinedUserErrors = string.Join(" | ", userErrorMessages);
                                _logger.LogError("[ShopifyUpdateService] User errors for variant {VariantId}: {Errors}", item.VariantId, combinedUserErrors);
                                errors.Add($"Variant {item.VariantId}: {combinedUserErrors}");
                                continue;
                            }

                            if (setQuantities.TryGetProperty("inventoryAdjustmentGroup", out var adjustmentGroup) && 
                                adjustmentGroup.TryGetProperty("changes", out var changes) &&
                                changes.GetArrayLength() > 0)
                            {
                                _logger.LogInformation("[ShopifyUpdateService] Successfully updated inventory for variant {VariantId} at location {LocationId}", 
                                    item.VariantId, item.LocationId);
                                results.Add(new
                                {
                                    VariantId = item.VariantId,
                                    LocationId = item.LocationId,
                                    Success = true,
                                    UpdatedQuantity = item.AvailableQuantity
                                });
                            }
                        }
                    }
                    catch (Exception itemEx)
                    {
                        var error = $"Error processing variant {item.VariantId}: {itemEx.Message}";
                        _logger.LogError(itemEx, "[ShopifyUpdateService] {Error}", error);
                        errors.Add(error);
                    }
                }

                var response = new
                {
                    TotalProcessed = request.InventoryItems.Count,
                    SuccessCount = results.Count,
                    ErrorCount = errors.Count,
                    Results = results,
                    Errors = errors
                };

                var responseJson = JsonSerializer.Serialize(response);
                
                if (errors.Any())
                {
                    _logger.LogWarning("[ShopifyUpdateService] UpdateInventoryLevelsAsync completed with {ErrorCount} errors out of {Total} items", 
                        errors.Count, request.InventoryItems.Count);
                }
                else
                {
                    _logger.LogInformation("[ShopifyUpdateService] UpdateInventoryLevelsAsync completed successfully for all {Count} items", 
                        request.InventoryItems.Count);
                }

                return responseJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateService] Error occurred in UpdateInventoryLevelsAsync: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateInventoryLevelsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<string> GetInventoryItemIdFromVariantAsync(HttpClient client, long variantId)
        {
            _logger.LogDebug("[ShopifyUpdateService] GetInventoryItemIdFromVariantAsync called for variant: {VariantId}", variantId);
            
            try
            {
                var query = @"
                    query($id: ID!) {
                      productVariant(id: $id) {
                        inventoryItem {
                          id
                        }
                      }
                    }";

                var variables = new
                {
                    id = $"gid://shopify/ProductVariant/{variantId}"
                };

                var requestBody = new
                {
                    query,
                    variables
                };

                var response = await client.PostAsync("", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();

                using var jsonDoc = JsonDocument.Parse(responseContent);

                if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("productVariant", out var variant) &&
                    variant.TryGetProperty("inventoryItem", out var inventoryItem) &&
                    inventoryItem.TryGetProperty("id", out var id))
                {
                    var inventoryItemId = id.GetString();
                    _logger.LogDebug("[ShopifyUpdateService] Found inventory item ID: {InventoryItemId} for variant: {VariantId}", inventoryItemId, variantId);
                    return inventoryItemId;
                }

                _logger.LogWarning("[ShopifyUpdateService] No inventory item found for variant: {VariantId}", variantId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateService] Error getting inventory item ID for variant {VariantId}: {Message}", variantId, ex.Message);
                return null;
            }
        }
    }
}