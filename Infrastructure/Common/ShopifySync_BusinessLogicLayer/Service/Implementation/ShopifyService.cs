using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Modes.Gcm;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO;
using ShopifySync_DataAccessLayer.Entities.DTOs.NewFolder;
using ShopifySync_DataAccessLayer.Entities.DTOs.OptionSetDTO;
using ShopifySync_DataAccessLayer.Entities.DTOs.TagSetDTO;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Text;
using System.Text.Json;
using System.Transactions;
using System.Xml.Linq;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class ShopifyService : IShopifyService
    {
        private readonly ICommonService _commonService;
        private readonly ILogger<ShopifyService> _logger;
        private readonly string _token;
        private readonly string _shopUrl;
        private readonly string _version;

        private readonly int _productPageSize;
        private readonly int _variantPageSize;
        private readonly int _variantMetaFieldPageSize;
        private readonly int _inventoryLevelPageSize;
        private readonly int _collectionPageSize;
        private readonly int _locationPageSize;
        private readonly int _vendorPageSize;
        private readonly int _concurrentLimit;
        private readonly IServiceProvider _serviceProvider;

        private readonly PageCursorTracker _tracker;
        private readonly IShopifyRepository _shopifyRepo;
        private readonly IShopifyUpdateService _shopifyUpdateService;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly IWebHookRMQService _webHookRMQService;
        private readonly IShopifyUpdateRMQService _shopifyUpdateRMQService;
        const string variPriceNameSpace = "gnt_vari_price";
        const string customNameSpace = "custom";
        const string oemKey = "oem";
        const string oemcomplementairesKey = "oem_complementaires";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShopifyService"/> class.
        /// </summary>
        /// <param name="settings">Shopify settings options.</param>
        /// <param name="tracker">Page cursor tracker for pagination.</param>
        /// <param name="shopifyRepo">Shopify repository for data operations.</param>
        /// <param name="serviceProvider">Service provider for dependency injection.</param>
        /// <param name="commonService">Common service for logging.</param>
        /// <param name="logger">Logger for application logging.</param>
        /// <param name="shopifyUpdate">Shopify update service for updating products.</param>
        /// <param name="transactionHistoryService">Transaction history service for logging.</param>
        /// <param name="webHookRMQService">RabbitMQ service for inbound webhooks.</param>
        /// <param name="shopifyUpdateRMQService">RabbitMQ service for outbound updates.</param>
        public ShopifyService(
            IOptions<ShopifySetting> settings, 
            PageCursorTracker tracker, 
            IShopifyRepository shopifyRepo, 
            IServiceProvider serviceProvider, 
            ICommonService commonService, 
            ILogger<ShopifyService> logger, 
            IShopifyUpdateService shopifyUpdate, 
            ITransactionHistoryService transactionHistoryService,
            IWebHookRMQService webHookRMQService,
            IShopifyUpdateRMQService shopifyUpdateRMQService)
        {
            _commonService = commonService;
            _logger = logger;
            _token = settings.Value.Token;
            _shopUrl = $"{settings.Value.ShopName}.myshopify.com";
            _version = settings.Value.Version;
            _productPageSize = settings.Value.ProductPageSize;
            _variantPageSize = settings.Value.VariantPageSize;
            _variantMetaFieldPageSize = settings.Value.VariantMetaFieldPageSize;
            _inventoryLevelPageSize = settings.Value.InventoryLevelPageSize;
            _collectionPageSize = settings.Value.CollectionPageSize;
            _locationPageSize = settings.Value.LocationPageSize;
            _vendorPageSize = settings.Value.VendorPageSize;
            _tracker = tracker;
            _shopifyRepo = shopifyRepo;
            _serviceProvider = serviceProvider;
            _concurrentLimit = settings.Value.ConcurrencyLimit;
            _shopifyUpdateService = shopifyUpdate;
            _transactionHistoryService = transactionHistoryService;
            _webHookRMQService = webHookRMQService;
            _shopifyUpdateRMQService = shopifyUpdateRMQService;

            _logger.LogInformation("[ShopifyService] Service initialized successfully with shop URL: {ShopUrl}", _shopUrl);
        }

        /// <summary>
        /// Imports collections from Shopify for the specified page.
        /// </summary>
        /// <param name="page">The page number to import collections for.</param>
        public async Task ImportCollectionsAsync(int page)
        {
            _logger.LogInformation("[ShopifyService] Starting ImportCollectionsAsync for page {Page}", page);
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyService] HTTP client configured for shop: {ShopUrl}", _shopUrl);

                string afterCursor = page > 1 ? _tracker.PageCursors.GetValueOrDefault(page - 1) : null;
                _logger.LogDebug("[ShopifyService] Using cursor for page {Page}: {Cursor}", page, afterCursor ?? "null");

                var query = @"
        query ($first: Int!, $after: String) {
            collections(first: $first, after: $after) {
                pageInfo {
                    hasNextPage
                    endCursor
                }
                edges {
                    node {
                        id
                        title
                        description
                        handle
                    }
                }
            }
        }";

                _tracker.PageSize = _collectionPageSize;
                var variables = new
                {
                    first = _tracker.PageSize,
                    after = afterCursor
                };

                var requestBody = new { query, variables };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                _logger.LogDebug("[ShopifyService] Sending GraphQL request for collections, page size: {PageSize}", _tracker.PageSize);
                var response = await client.PostAsync("", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[ShopifyService] GraphQL request failed with status: {StatusCode}, response: {Response}", response.StatusCode, json);
                    throw new Exception($"GraphQL request failed: {response.StatusCode}");
                }

                _logger.LogDebug("[ShopifyService] GraphQL response received successfully, length: {Length}", json.Length);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement.GetProperty("data").GetProperty("collections");

                string nextCursor = root.GetProperty("pageInfo").GetProperty("endCursor").GetString();
                if (nextCursor != null && !_tracker.PageCursors.ContainsKey(page))
                {
                    _tracker.PageCursors[page] = nextCursor;
                    _logger.LogDebug("[ShopifyService] Saved cursor for page {Page}: {Cursor}", page, nextCursor);
                }

                var collections = new List<Collection>();
                var edges = root.GetProperty("edges").EnumerateArray();
                int collectionCount = 0;

                foreach (var edge in edges)
                {
                    var node = edge.GetProperty("node");
                    var collection = new Collection
                    {
                        ShopifyId = node.GetProperty("id").GetString(),
                        Title = node.GetProperty("title").GetString(),
                        Description = node.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Image = node.TryGetProperty("image", out var img) && img.ValueKind != JsonValueKind.Null && img.TryGetProperty("src", out var src) ? src.GetString() : null,

                    };

                    collections.Add(collection);
                    collectionCount++;
                }

                _logger.LogInformation("[ShopifyService] Processed {Count} collections for page {Page}", collectionCount, page);
                await _shopifyRepo.AddCollectionsAsync(collections);
                _logger.LogInformation("[ShopifyService] Successfully saved {Count} collections to repository for page {Page}", collectionCount, page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred while importing collections for page {Page}: {Message}", page, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportCollectionsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Imports all locations from Shopify.
        /// </summary>
        public async Task ImportLocationsAsync()
        {
            _logger.LogInformation("[ShopifyService] Starting ImportLocationsAsync");
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyService] HTTP client configured for locations import");

                string afterCursor = null;
                bool hasNextPage = true;
                int currentPage = 1;
                var locations = new List<Location>();
                int totalLocations = 0;

                while (hasNextPage)
                {
                    _logger.LogDebug("[ShopifyService] Processing locations page {Page}", currentPage);

                    var query = @"
        query ($first: Int!, $after: String) {
            locations(first: $first, after: $after) {
                pageInfo {
                    hasNextPage
                    endCursor
                }
                edges {
                    node {
                        id
                        name
                        address {
                            city
                            country
                            province
                            zip
                        }
                    }
                }
            }
        }"
                    ;

                    _tracker.PageSize = _locationPageSize;
                    var variables = new
                    {
                        first = _tracker.PageSize,
                        after = afterCursor
                    };

                    var requestBody = new { query, variables };
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("", content);
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("[ShopifyService] GraphQL request failed for locations page {Page} with status: {StatusCode}", currentPage, response.StatusCode);
                        throw new Exception($"GraphQL request failed for locations: {response.StatusCode}");
                    }

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement.GetProperty("data").GetProperty("locations");

                    hasNextPage = root.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
                    afterCursor = root.GetProperty("pageInfo").GetProperty("endCursor").GetString();

                    if (afterCursor != null && !_tracker.PageCursors.ContainsKey(currentPage))
                    {
                        _tracker.PageCursors[currentPage] = afterCursor;
                        _logger.LogDebug("[ShopifyService] Saved cursor for locations page {Page}: {Cursor}", currentPage, afterCursor);
                    }

                    int pageLocationCount = 0;
                    foreach (var edge in root.GetProperty("edges").EnumerateArray())
                    {
                        var node = edge.GetProperty("node");
                        var addressNode = node.GetProperty("address");

                        var location = new Location
                        {
                            ShopifyId = node.GetProperty("id").GetString().Split('/').Last(),
                            Name = node.GetProperty("name").GetString(),
                            City = addressNode.TryGetProperty("city", out var city) ? city.GetString() : null,
                            Province = addressNode.TryGetProperty("province", out var province) ? province.GetString() : null,
                            Country = addressNode.TryGetProperty("country", out var country) ? country.GetString() : null,
                            Zip = addressNode.TryGetProperty("zip", out var zip) ? zip.GetString() : null,
                        };

                        locations.Add(location);
                        pageLocationCount++;
                    }

                    totalLocations += pageLocationCount;
                    _logger.LogDebug("[ShopifyService] Processed {Count} locations for page {Page}", pageLocationCount, currentPage);
                    currentPage++;
                }

                if (locations != null && locations.Any())
                {
                    _logger.LogInformation("[ShopifyService] Saving {Count} locations to repository", locations.Count);
                    await _shopifyRepo.AddLocationsAsync(locations);
                    _logger.LogInformation("[ShopifyService] Successfully saved {Count} locations to repository", locations.Count);
                }
                else
                {
                    _logger.LogWarning("[ShopifyService] No locations found to save");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred while importing locations: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportLocationsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Imports all vendors from Shopify.
        /// </summary>
        public async Task ImportVendorsAsync()
        {
            _logger.LogInformation("[ShopifyService] Starting ImportVendorsAsync");
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyService] HTTP client configured for vendors import");

                string afterCursor = null;
                bool hasNextPage = true;
                int currentPage = 1;
                var vendorsList = new List<Vendor>();
                int totalVendors = 0;

                while (hasNextPage)
                {
                    _logger.LogDebug("[ShopifyService] Processing vendors page {Page}", currentPage);

                    var query = @"
                    query ($first: Int!, $after: String) {
                        products(first: $first, after: $after) {
                            pageInfo {
                                hasNextPage
                                endCursor
                            }
                            edges {
                                node {
                                    vendor 
                                }
                            }
                        }
                    }";

                    _tracker.PageSize = _vendorPageSize;
                    var variables = new
                    {
                        first = _tracker.PageSize,
                        after = afterCursor
                    };

                    var requestBody = new { query, variables };
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("", content);
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("[ShopifyService] GraphQL request failed for vendors page {Page} with status: {StatusCode}", currentPage, response.StatusCode);
                        throw new Exception($"GraphQL request failed for vendors: {response.StatusCode}");
                    }

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement.GetProperty("data").GetProperty("products");

                    hasNextPage = root.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
                    afterCursor = root.GetProperty("pageInfo").GetProperty("endCursor").GetString();

                    if (afterCursor != null && !_tracker.PageCursors.ContainsKey(currentPage))
                    {
                        _tracker.PageCursors[currentPage] = afterCursor;
                        _logger.LogDebug("[ShopifyService] Saved cursor for vendors page {Page}: {Cursor}", currentPage, afterCursor);
                    }

                    var vendors = root.GetProperty("edges")
                        .EnumerateArray()
                        .Select(edge => edge.GetProperty("node").GetProperty("vendor").GetString())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct()
                        .Select(v => new Vendor { Title = v })
                        .ToList();

                    vendorsList.AddRange(vendors);
                    totalVendors += vendors.Count;
                    _logger.LogDebug("[ShopifyService] Found {Count} vendors on page {Page}", vendors.Count, currentPage);

                    currentPage++;
                }

                if (vendorsList != null && vendorsList.Any())
                {
                    _logger.LogInformation("[ShopifyService] Saving {Count} vendors to repository", vendorsList.Count);
                    await _shopifyRepo.AddVendorsAsync(vendorsList);
                    _logger.LogInformation("[ShopifyService] Successfully saved {Count} vendors to repository", vendorsList.Count);
                }
                else
                {
                    _logger.LogWarning("[ShopifyService] No vendors found to save");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred while importing vendors: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportVendorsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Imports all products and their children from Shopify in parallel, transactional, and incremental manner.
        /// </summary>
        /// <param name="updatedAfter">Optional filter for products updated after this date.</param>
        public async Task ImportAllProductsAndChildrenAsync(DateTime? updatedAfter)
        {
            _logger.LogInformation("[ShopifyService] Starting ImportAllProductsAndChildrenAsync with filter: {Filter}", updatedAfter?.ToString("yyyy-MM-dd HH:mm:ss") ?? "none");

            DateTime startAt = DateTime.UtcNow;
            var errors = new List<string>();
            var numberOfCursorsProcessed = 0;
            var numberOfCursorsFailed = 0;

            List<string> cursors = new List<string>();
            var pageResults = new List<(int imported, string? error)>();
            int concurrencyLimit = _concurrentLimit;
            int historyId = 0;
            int imported = 0;
            string filter = updatedAfter.HasValue ? $"created_at:>{updatedAfter} OR updated_at:>{updatedAfter.Value}" : "";

            _logger.LogDebug("[ShopifyService] Using concurrency limit: {ConcurrencyLimit}", concurrencyLimit);

            // 1. Fetch all cursors (simulate by iterating sequentially, but only collect cursors)
            try
            {
                _logger.LogDebug("[ShopifyService] Fetching cursors for filter: {Filter}", filter);
                cursors = await GetCursors(filter, updatedAfter);
                _logger.LogInformation("[ShopifyService] Successfully fetched {Count} cursors", cursors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Failed to collect cursors: {Message}", ex.Message);
                errors.Add(ex.Message);
                await _shopifyRepo.AddHistoryInventory(new HistoryInventory() { InProgress = false, IsSuccess = false, LastSyncDate = DateTime.UtcNow, TotalProductsProcessed = 0, LastRecordDate = null, ErrorMessage = $"Failed to collect cursors: {ex.Message}" });
                throw;
            }

            _logger.LogDebug("[ShopifyService] Creating initial history record for {CursorCount} cursors", cursors.Count);
            historyId = await _shopifyRepo.AddHistoryInventory(new HistoryInventory()
            {
                IsSuccess = false,
                LastSyncDate = DateTime.UtcNow,
                ProcessStartDate = DateTime.UtcNow,
                TotalProductsProcessed = 0,
                TotalCursorsFailed = 0,
                TotalCursorsProcessed = 0,
                TotalCursors = cursors.Count,
                LastRecordDate = null,
                ErrorMessage = null,
                InProgress = true
            });
            _logger.LogDebug("[ShopifyService] Created history record with ID: {HistoryId}", historyId);

            // 2. Process each page in parallel (with concurrency limit)

            // cursors.Select(async (cursor, idx) =>
            _logger.LogInformation("[ShopifyService] Starting to process {CursorCount} cursors", cursors.Count);
            foreach (var cursor in cursors)
            {
                _logger.LogDebug("[ShopifyService] Processing cursor: {Cursor}", cursor);

                string? error = null;
                DateTime? lastRecordDate = null;
                //using (var service = _serviceProvider.CreateScope())
                //{
                //    var shopifyRepo = service.ServiceProvider.GetRequiredService<IShopifyRepository>();
                try
                {
                    //using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    //{
                    _logger.LogDebug("[ShopifyService] Fetching products for cursor: {Cursor}", cursor);
                    JsonElement? root = await GetProductsFromShopify(cursor, filter);
                    if (root == null)
                    {
                        numberOfCursorsFailed++;
                        error = $"Failed to fetch products for cursor: {cursor}";
                        _logger.LogWarning("[ShopifyService] Failed to fetch products for cursor: {Cursor}", cursor);
                        continue;
                    }

                    var products = new List<Product>();
                    var variants = new List<Variant>();
                    var collections = new List<Collection>();
                    var tags = new List<Tag>();
                    var productTags = new List<ProductTag>();
                    var productCollections = new List<ProductCollection>();
                    var options = new List<Option>();
                    var productOptions = new List<ProductOption>();
                    var inventoryLevels = new List<InventoryLevel>();
                    var optionValues = new List<OptionValue>();
                    var variantOptionValues = new List<VariantOptionValue>();

                    foreach (var edge in root.Value.GetProperty("edges").EnumerateArray())
                    {
                        var node = edge.GetProperty("node");
                        var shopifyProductId = node.GetProperty("id").GetString();
                        var productId = node.GetProperty("id").GetString().Split('/').Last();

                        var dbProduct = await _shopifyRepo.GetProductByShopifyIdAsync(shopifyProductId);
                        if (dbProduct == null)
                        {
                            Product edgeProduct = await SetProductAsync(node, shopifyProductId);
                            lastRecordDate = edgeProduct.CreatedAt;
                            products.Add(edgeProduct);

                            var optionDto = await SetOptionProductOptionAndOptionValueAsync(node, edgeProduct);
                            if (optionDto != null)
                            {
                                options.AddRange(optionDto.Options);
                                productOptions.AddRange(optionDto.ProductOptions);
                                optionValues.AddRange(optionDto.OptionValues);
                            }

                            if (node.TryGetProperty("variants", out var variantsNode))
                            {
                                var variantEdges = variantsNode.GetProperty("edges").EnumerateArray();
                                if (variantEdges.Any())
                                {
                                    foreach (var variantEdge in variantEdges)
                                    {
                                        var variantNode = variantEdge.GetProperty("node");
                                        Variant variant = await SetVariant(variantNode, edgeProduct);
                                        variants.Add(variant);

                                        var variantOptionValue = await SetVariantOptionValueAsync(optionValues, variantNode, variant);
                                        variantOptionValues.AddRange(variantOptionValue);

                                        if (variantNode.TryGetProperty("inventoryItem", out var inventoryItemNode) && inventoryItemNode.TryGetProperty("inventoryLevels", out var inventoryLevelsNode))
                                        {
                                            var inventoryLevelsData = await AddLocationAndSetInventoryLevelAsync(node, variant, inventoryLevelsNode);
                                            if (inventoryLevelsData != null && inventoryLevelsData.Any())
                                            {
                                                inventoryLevels.AddRange(inventoryLevelsData);
                                            }
                                        }
                                    }
                                }
                            }

                            var tagSetDto = await SetTagAndProductTagAsync(node, edgeProduct);
                            if (tagSetDto != null)
                            {
                                tags.AddRange(tagSetDto.Tags);
                                productTags.AddRange(tagSetDto.ProductTags);
                            }

                            if (node.TryGetProperty("collections", out var collectionsNode))
                            {
                                var collectionsDto = await SetCollectionAndProductCollectionAsync(collectionsNode, edgeProduct);
                                if (collectionsDto != null)
                                {
                                    productCollections.AddRange(collectionsDto.ProductCollections);
                                    collections.AddRange(collectionsDto.Collections);
                                }
                            }
                        }
                        else
                        {
                            await SetProductAndChildrenToUpdateAsync(node, dbProduct);
                            var updatedProduct = await UpdateProductAsync(dbProduct);
                            lastRecordDate = updatedProduct.UpdatedAt;
                            imported++;

                            await _transactionHistoryService.LogSuccessTransactionAsync(
                            shopifyProductId,
                            "product",
                            "ShopifyService",
                           root.ToString(),
                           "Product updated successfully via bulk import",
                                new Dictionary<string, object>
                                {
                                    ["Operation"] = "ImportAllProductsAndChildrenAsync",
                                    ["Action"] = "BulkImport",
                                    ["Cursor"] = cursor,
                                    ["ProductCount"] = products.Count
                                });
                        }

                        await SaveShopifyProductDataAsync(products, variants, options, productOptions, optionValues, variantOptionValues, tags, productTags, collections, productCollections, inventoryLevels, productId: productId);
                        
                        foreach (var product in products)
                        {
                            await _transactionHistoryService.LogSuccessTransactionAsync(
                                product.ShopifyId,
                                "product",
                                "ShopifyService",
                                null, // Don't log full payload for bulk operations
                                "Product imported successfully via bulk import",
                                new Dictionary<string, object> { 
                                    ["Operation"] = "ImportAllProductsAndChildrenAsync", 
                                    ["Action"] = "BulkImport",
                                    ["Cursor"] = cursor,
                                    ["ProductCount"] = products.Count
                                });
                        }
                        
                    }

                    //scope.Complete();
                    imported += products.Count;
                    numberOfCursorsProcessed++;
                    // }
                }
                catch (Exception ex)
                { 
                  //  await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                  //  {
                  //      Cursor = cursor,
                  //      LastError = ex.StackTrace ?? ex.Message,
                  //      Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                  //      RetryCount = 0,
                  //      LastAttemptAt = null,
                  //      Filter = filter
                  //  }
                  //);
                    numberOfCursorsFailed++;
                    _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportAllProductsAndChildrenAsync", 1, ex.Message, ex.ToString());
                    errors.Add(ex.Message);
                    error = ex.Message;
                }
                finally
                {
                    await _shopifyRepo.UpdateHistoryInventory(historyId, new HistoryInventory()
                    {
                        IsSuccess = error == null,
                        LastSyncDate = DateTime.UtcNow,
                        TotalProductsProcessed = imported,
                        TotalCursorsProcessed = numberOfCursorsProcessed,
                        TotalCursorsFailed = numberOfCursorsFailed,
                        LastRecordDate = lastRecordDate,
                        ErrorMessage = error,
                        InProgress = true
                    });

                    if (error != null) errors.Add(error);
                }
                // }
            };

            var totalTime = DateTime.UtcNow - startAt;
            if (!(errors != null && errors.Any()))
            {
                _logger.LogInformation("[ShopifyService] ImportAllProductsAndChildrenAsync completed successfully in {Duration}. Processed {Processed} cursors, {Failed} failed",
                    totalTime, numberOfCursorsProcessed, numberOfCursorsFailed);
                await _shopifyRepo.StatusUpdateHistoryInventory(historyId, new HistoryInventory()
                {
                    IsSuccess = true,
                    InProgress = false
                });
            }
            else
            {
                _logger.LogWarning("[ShopifyService] ImportAllProductsAndChildrenAsync completed with {ErrorCount} errors in {Duration}. Processed {Processed} cursors, {Failed} failed",
                    errors.Count, totalTime, numberOfCursorsProcessed, numberOfCursorsFailed);
            }
        }

        private async Task<JsonElement?> GetProductsFromShopify(string cursor, string filter)
        {
            _logger.LogDebug("[ShopifyService] GetProductsFromShopify called with cursor: {Cursor}, filter: {Filter}", cursor, filter);
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);

                var query = $@"
            query ($first: Int!, $after: String) {{
              products(first: $first, after: $after, query: ""{filter}"") {{
                pageInfo {{ hasNextPage endCursor }}
                edges {{
                  node {{
                    id title handle descriptionHtml vendor status createdAt updatedAt  
                    compareAtPriceRange {{ minVariantCompareAtPrice{{ amount }} maxVariantCompareAtPrice{{ amount }} }}
                    tags productType options {{ id name values position}}
                                    variants(first: {_variantPageSize}) {{ edges {{ node {{ id title sku price compareAtPrice barcode  createdAt updatedAt metafields(first: {_variantMetaFieldPageSize}) {{ edges {{ node {{ id namespace key value type }} }} }} inventoryQuantity selectedOptions {{ name value }} inventoryItem {{ inventoryLevels(first: {_inventoryLevelPageSize}) {{ edges {{ node {{ updatedAt location {{ id name address {{ city country province zip }} }} }} }} }} }} }} }} }}
                                    collections(first: {_collectionPageSize}) {{ edges {{ node {{ id title description handle image {{ src }} }} }} }}
                          }}
                                }}
                              }}
                            }}";
                var variables = new { first = _productPageSize, after = cursor };
                var requestBody = new { query, variables };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[ShopifyService] GraphQL request failed with status: {StatusCode} {ReasonPhrase} for cursor: {Cursor}",
                        response.StatusCode, response.ReasonPhrase, cursor);
                    await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                    {
                        Cursor = cursor,
                        LastError = response.ReasonPhrase,
                        Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                        RetryCount = 0,
                        LastAttemptAt = null,
                        Filter = filter
                    }
                    );
                    var errorMsg = $"Shopify API error: {(int)response.StatusCode} {response.ReasonPhrase}";
                    _commonService.ErrorLogs(errorMsg, "GetProductsFromShopify", 1, "Failed to fetch products", response.ToString());
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement.Clone();

                if (!root.TryGetProperty("data", out JsonElement dataElement) ||
                    !dataElement.TryGetProperty("products", out JsonElement productsElement))
                {
                    await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                    {
                        Cursor = cursor,
                        LastError = "Invalid or missing product data",
                        Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                        RetryCount = 0,
                        LastAttemptAt = null,
                        Filter = filter
                    }
                    );

                    _commonService.ErrorLogs("Invalid or missing product data", "GetProductsFromShopify", 2, "Product data not found", json);
                    return null;
                }

                return productsElement;
            }
            catch (Exception ex)
            {
                await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                {
                    Cursor = cursor,
                    LastError = ex.StackTrace ?? ex.Message,
                    Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                    RetryCount = 0,
                    LastAttemptAt = null,
                    Filter = filter
                });

                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetProductsFromShopify", 1, ex.Message, ex.ToString());
                return null;
            }
        }

        private async Task<List<string>> GetCursors(string filter, DateTime? updatedAfter)
        {
            _logger.LogDebug("[ShopifyService] GetCursors called with filter: {Filter}, updatedAfter: {UpdatedAfter}",
                filter, updatedAfter?.ToString("yyyy-MM-dd HH:mm:ss") ?? "none");
            try
            {
                var cursors = new List<string?> { null }; // Start with null for the first page
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);

                string afterCursor = null;
                bool hasNextPage = true;
                int pageCount = 0;
                while (hasNextPage)
                {
                    var query = $@"
                        query ($first: Int!, $after: String) {{
                          products(first: $first, after: $after,  query: ""{filter}"") {{
                            pageInfo {{
                              hasNextPage
                              endCursor
                            }}
                            edges {{ node {{ id }} }}
                          }}
                        }}";

                    var variables = new { first = _productPageSize, after = afterCursor };
                    var requestBody = new { query, variables };
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("", content);
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement.GetProperty("data").GetProperty("products");
                    hasNextPage = root.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
                    afterCursor = root.GetProperty("pageInfo").GetProperty("endCursor").GetString();
                    if (hasNextPage && afterCursor != null)
                        cursors.Add(afterCursor);
                    pageCount++;
                }
                _logger.LogDebug("[ShopifyService] GetCursors completed, found {CursorCount} cursors across {PageCount} pages", cursors.Count, pageCount);
                return cursors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in GetCursors: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetCursors", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Gets the status of the history inventory from Shopify.
        /// </summary>
        /// <returns>The latest history inventory record.</returns>
        public async Task<Response> GetHistoryStatus()
        {
            _logger.LogDebug("[ShopifyService] GetHistoryStatus called");
            try
            {
                var data = await _shopifyRepo.GetHistoryStatus();

                if (data == null)
                {
                    _logger.LogWarning("[ShopifyService] HistoryStatus not found");
                    return ResponseHelper.NotFound("HistoryStatus not found");
                }

                _logger.LogDebug("[ShopifyService] HistoryStatus fetched successfully");
                return ResponseHelper.Success("HistoryStatus fetched successfully", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in GetHistoryStatus: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetHistoryStatus", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Gets paginated history status records with filtering support.
        /// </summary>
        /// <param name="filterDto">Filter criteria including pagination parameters.</param>
        /// <returns>A Response object containing the paginated list of history records and pagination info.</returns>
        public async Task<Response> GetHistoryStatusPaginated(HistoryInventoryFilterDto filterDto)
        {
            _logger.LogInformation("[ShopifyService] Attempting to get paginated history status with filter: {FilterDto}", filterDto);
            try
            {
                var data = await _shopifyRepo.GetHistoryStatusPaginated(filterDto);
                PaginationInfo pagination = PaginatedHistoryStatus(filterDto, data);
                return ResponseHelper.Success("History status records fetched successfully", data.Data, null, pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in GetHistoryStatusPaginated: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetHistoryStatusPaginated", 1, ex.Message, ex.ToString());
                throw;
            }
        }


        /// <summary>
        /// Imports products, locations, and vendors from Shopify.
        /// </summary>
        /// <param name="updatedAfter">Optional filter for products updated after this date.</param>
        public async Task ImportProducts(DateTime? updatedAfter)
        {
            _logger.LogInformation("[ShopifyService] Starting ImportProducts with filter: {Filter}", updatedAfter?.ToString("yyyy-MM-dd HH:mm:ss") ?? "none");
            try
            {
                _logger.LogInformation("[ShopifyService] Importing locations");
                await ImportLocationsAsync();

                _logger.LogInformation("[ShopifyService] Importing vendors");
                await ImportVendorsAsync();

                _logger.LogInformation("[ShopifyService] Importing products and children");
                await ImportAllProductsAndChildrenAsync(updatedAfter);

                _logger.LogInformation("[ShopifyService] Importing product images");
                await ImportProductImagesAsync(updatedAfter);

                _logger.LogInformation("[ShopifyService] ImportProducts completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in ImportProducts: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProducts", 1, ex.Message, ex.ToString());
                throw;
            }
        }
        /// <summary>
        /// Imports a single product by its Shopify product ID.
        /// </summary>
        /// <param name="productId">The Shopify product ID.</param>
        /// <returns>A string indicating the result of the import.</returns>
        public async Task<string> ImportProductByIdAsync(long productId)
        {
            _logger.LogInformation("[ShopifyService] Starting ImportProductByIdAsync for product ID: {ProductId}", productId);
            try
            {
                _logger.LogDebug("[ShopifyService] Fetching product data from Shopify for ID: {ProductId}", productId);
                string json = await ImportProductFromShopifyAsync(productId.ToString());

                if (json != null && !json.Contains("error"))
                {
                    _logger.LogDebug("[ShopifyService] Product data received for ID: {ProductId}, parsing JSON", productId);
                    using var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("data", out var dataElement) ||
                        !dataElement.TryGetProperty("product", out var productElement) ||
                        productElement.ValueKind == JsonValueKind.Null)
                    {
                        _logger.LogWarning("[ShopifyService] Invalid or missing product data for ID: {ProductId}", productId);
                        _commonService.ErrorLogs("Invalid or missing product data", "ImportProductByIdAsync", 2, "Product not found", json);
                        return null;
                    }

                    var shopifyProductId = productElement.GetProperty("id").GetString();
                    _logger.LogDebug("[ShopifyService] Checking if product {ProductId} exists in database", productId);
                    var dbProduct = await _shopifyRepo.GetProductByShopifyIdAsync(shopifyProductId);
                    if (dbProduct != null)
                    {
                        _logger.LogInformation("[ShopifyService] Product {ProductId} exists, updating existing product", productId);
                        await SetProductAndChildrenToUpdateAsync(productElement, dbProduct);
                        var updatedProduct = await UpdateProductAsync(dbProduct);
                        await SyncProductImagesAsync(dbProduct, productElement);
                        _logger.LogInformation("[ShopifyService] Product {ProductId} updated successfully", productId);
                        
                        await _transactionHistoryService.LogSuccessTransactionAsync(
                            shopifyProductId,
                            "product",
                            "ShopifyService",
                            json,
                            "Product updated successfully",
                            new Dictionary<string, object> { 
                                ["Operation"] = "ImportProductByIdAsync", 
                                ["Action"] = "Update",
                                ["ProductId"] = productId.ToString()
                            });
                        
                        return "Product updated Successfully.";
                    }

                    _logger.LogInformation("[ShopifyService] Product {ProductId} not found, creating new product", productId);
                    var product = await SetProductAsync(productElement, shopifyProductId);

                    var products = new List<Product>();
                    var variants = new List<Variant>();
                    var options = new List<Option>();
                    var productOptions = new List<ProductOption>();
                    var optionValues = new List<OptionValue>();
                    var variantOptionValues = new List<VariantOptionValue>();
                    var tags = new List<Tag>();
                    var productTags = new List<ProductTag>();
                    var collections = new List<Collection>();
                    var productCollections = new List<ProductCollection>();
                    var inventoryLevels = new List<InventoryLevel>();

                    var optionDto = await SetOptionProductOptionAndOptionValueAsync(productElement, product);

                    if (optionDto != null)
                    {
                        options.AddRange(optionDto.Options);
                        productOptions.AddRange(optionDto.ProductOptions);
                        optionValues.AddRange(optionDto.OptionValues);
                    }

                    if (productElement.TryGetProperty("variants", out var variantsNode))
                    {
                        var variantEdges = variantsNode.GetProperty("edges").EnumerateArray();
                        foreach (var variantEdge in variantEdges)
                        {
                            var variantNode = variantEdge.GetProperty("node");
                            var variant = await SetVariant(variantNode, product);
                            variants.Add(variant);


                            var variantOptionValue = await SetVariantOptionValueAsync(optionValues, variantNode, variant);
                            variantOptionValues.AddRange(variantOptionValue);

                            if (variantNode.TryGetProperty("inventoryItem", out var inventoryItemNode) &&
                                inventoryItemNode.TryGetProperty("inventoryLevels", out var inventoryLevelsNode))
                            {
                                var inventoryLevelsData = await AddLocationAndSetInventoryLevelAsync(productElement, variant, inventoryLevelsNode);
                                if (inventoryLevelsData != null && inventoryLevelsData.Any())
                                {
                                    inventoryLevels.AddRange(inventoryLevelsData);
                                }
                            }
                        }
                    }

                    var tagSetDto = await SetTagAndProductTagAsync(productElement, product);
                    if (tagSetDto != null)
                    {
                        tags.AddRange(tagSetDto.Tags);
                        productTags.AddRange(tagSetDto.ProductTags);
                    }

                    if (productElement.TryGetProperty("collections", out var collectionsNode))
                    {
                        var collectionsDto = await SetCollectionAndProductCollectionAsync(collectionsNode, product);
                        if (collectionsDto != null)
                        {
                            productCollections.AddRange(collectionsDto.ProductCollections);
                            collections.AddRange(collectionsDto.Collections);
                        }
                    }

                    products.Add(product);

                    _logger.LogDebug("[ShopifyService] Saving product data for ID: {ProductId}", productId);
                    await SaveShopifyProductDataAsync(products, variants, options, productOptions, optionValues, variantOptionValues, tags, productTags, collections, productCollections, inventoryLevels, productId: productId.ToString());

                    _logger.LogDebug("[ShopifyService] Adding product images for ID: {ProductId}", productId);
                    await AddProductImagesAsync(productElement);

                    _logger.LogInformation("[ShopifyService] Product {ProductId} imported successfully", productId);
                    
                    await _transactionHistoryService.LogSuccessTransactionAsync(
                        shopifyProductId,
                        "product",
                        "ShopifyService",
                        json,
                        "Product imported successfully",
                        new Dictionary<string, object> { 
                            ["Operation"] = "ImportProductByIdAsync", 
                            ["Action"] = "Create",
                            ["ProductId"] = productId.ToString()
                        });
                    
                    return "Product Imported Successfully.";
                }
                else if (json != null && json.Contains("error"))
                {
                    _logger.LogWarning("[ShopifyService] Got error while receiving product data for ID: {ProductId} JsonData:{json}", productId, json);
                    return json;
                }
                else
                {
                    _logger.LogWarning("[ShopifyService] No product data received for ID: {ProductId}, returning unauthorized message", productId);
                    return "StatusCode: 401, ReasonPhrase: 'Unauthorized'";
                }

            }
            catch (Exception ex)
            {
                var queueProduct =  await _shopifyRepo.GetProductFromQueueAsync(productId.ToString());

             if (queueProduct == null || queueProduct.RetryCount > 10)
                {
                    await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                    {
                        Cursor = "No Cursor Found",
                        ProductShopifyId = productId.ToString(),
                        LastError = ex.StackTrace ?? ex.Message,
                        Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                        RetryCount = 0,
                        LastAttemptAt = null,
                        Filter = "No Filter"
                    });
                }


                await _transactionHistoryService.LogFailedTransactionAsync(
                    $"gid://shopify/Product/{productId}",
                    "product",
                    "ShopifyService",
                    ex.Message,
                    null,
                    new Dictionary<string, object> { 
                        ["Operation"] = "ImportProductByIdAsync", 
                        ["Error"] = ex.Message,
                        ["ProductId"] = productId.ToString()
                    });

                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductByIdAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Synchronizes product images by comparing Shopify images with database images and adding only new ones.
        /// </summary>
        /// <param name="product">The product entity</param>
        /// <param name="productElement">The JSON element containing product data from Shopify</param>
        public async Task SyncProductImagesAsync(Product product, JsonElement productElement)
        {
            _logger.LogDebug("[ShopifyService] SyncProductImagesAsync called for product ID: {ProductId}", product.Id);
            try
            {
                var shopifyImageIds = new List<string>();
                if (productElement.TryGetProperty("images", out var imagesNode))
                {
                    shopifyImageIds = imagesNode.TryGetProperty("edges", out var edgesNode)
                                           ? edgesNode.EnumerateArray()
                                               .Select(edge => edge.GetProperty("node").GetProperty("id").GetString())
                                               .Where(id => !string.IsNullOrEmpty(id))
                                               .ToList()
                                           : new List<string>();
                }

                _logger.LogDebug("[ShopifyService] Found {Count} Shopify images for product {ProductId}", shopifyImageIds.Count, product.Id);

                var existingProductImages = await _shopifyRepo.GetProductImagesByProductIdAsync(product.Id);
                var existingImageShopifyIds = existingProductImages.Select(pi => pi.ImageShopifyId).ToHashSet();

                var newImageIds = shopifyImageIds.Where(id => !existingImageShopifyIds.Contains(id)).ToList();

                var newProductImages = new List<ProductImage>();
                if (productElement.TryGetProperty("images", out var imagesNodeForNew))
                {
                    foreach (var imageEdge in imagesNodeForNew.GetProperty("edges").EnumerateArray())
                    {
                        var imageNode = imageEdge.GetProperty("node");
                        var imageId = imageNode.GetProperty("id").GetString();

                        if (!string.IsNullOrEmpty(imageId) && newImageIds.Contains(imageId))
                        {
                            var productImage = new ProductImage
                            {
                                ProductId = product.Id,
                                ImageShopifyId = imageId,
                                ImageSrc = imageNode.GetProperty("src").GetString(),
                                LocalCreatedAt = DateTime.UtcNow
                            };
                            newProductImages.Add(productImage);
                        }
                    }
                }

                if (newProductImages.Any())
                {
                    _logger.LogInformation("[ShopifyService] Adding {Count} new images for product {ProductId}", newProductImages.Count, product.Id);
                    await _shopifyRepo.AddProductImagesAsync(newProductImages);
                    _logger.LogInformation("[ShopifyService] Successfully added {Count} new images for product {ProductId}", newProductImages.Count, product.Id);
                }
                else
                {
                    _logger.LogDebug("[ShopifyService] No new images to add for product {ProductId}", product.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in SyncProductImagesAsync for product {ProductId}: {Message}", product.Id, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SyncProductImagesAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }


        /// <summary>
        /// Imports a single product from Shopify by its product ID.
        /// </summary>
        /// <param name="productId">The Shopify product ID.</param>
        /// <returns>The raw JSON string of the product data.</returns>
        public async Task<string> ImportProductFromShopifyAsync(string productId)
        {
            try
            {
                var formattedProductId = $"gid://shopify/Product/{productId}";
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);

                var query = $@"
  query ($id: ID!) {{
    product(id: $id) {{
      id title handle descriptionHtml vendor status createdAt updatedAt 
      compareAtPriceRange {{ minVariantCompareAtPrice {{ amount }} maxVariantCompareAtPrice {{ amount }} }}
      tags productType  
      images(first: 50) {{
        edges {{
          node {{
            id
            src
          }}
        }}
      }}
      options {{ id name values position }}
      variants(first: {_variantPageSize}) {{
        edges {{
          node {{
            id title sku price compareAtPrice barcode createdAt updatedAt
            metafields(first: {_variantMetaFieldPageSize}) {{
              edges {{
                node {{ id namespace key value type }}
              }}
            }}
            inventoryQuantity
            selectedOptions {{ name value }}
            inventoryItem {{
              inventoryLevels(first: {_inventoryLevelPageSize}) {{
                edges {{
                  node {{
                    updatedAt
                    location {{
                      id
                      name
                      address {{ city country province zip }}
                    }}
                    quantities(names: [""available""]) {{
                      name
                      quantity
                    }}
                  }}
                }}
              }}
            }}
          }}
        }}
      }}
      collections(first: {_collectionPageSize}) {{
        edges {{
          node {{
            id
            title
            description
            handle
            image {{ src }}
          }}
        }}
      }}
    }}
  }}";


                var variables = new { id = formattedProductId };
                var requestBody = new { query, variables };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"Shopify API error: {(int)response.StatusCode} {response.ReasonPhrase}";
                    _commonService.ErrorLogs(errorMsg, "ImportProductFromShopifyAsync", 1, "Failed to fetch products", response.ToString());
                    return null;
                }
                var json = await response.Content.ReadAsStringAsync();
                return json;
            }
            catch (HttpRequestException ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductFromShopifyAsync", 1, ex.Message, ex.ToString());
                return null;
            }
            catch (JsonException ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductFromShopifyAsync", 1, ex.Message, ex.ToString());
                return null;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductFromShopifyAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SaveShopifyProductDataAsync(List<Product> products, List<Variant> variants, List<Option> options, List<ProductOption> productOptions, List<OptionValue> optionValues, List<VariantOptionValue> variantOptionValues, List<Tag> tags, List<ProductTag> productTags, List<Collection> collections, List<ProductCollection> productCollections, List<InventoryLevel> inventoryLevels, string cursor = "No Cursor Found", string? filter = null, string? productId = null)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    await _shopifyRepo.AddOptionsAsync(options);
                    await _shopifyRepo.AddOptionValuesAsync(optionValues);
                    await _shopifyRepo.AddProductsAsync(products);
                    await _shopifyRepo.AddVariants(variants);
                    await _shopifyRepo.AddTagsAsync(tags);
                    await _shopifyRepo.AddProductTagsAsync(productTags);
                    await _shopifyRepo.AddCollectionsAsync(collections);
                    await _shopifyRepo.AddProductCollectionsAsync(productCollections);
                    await _shopifyRepo.AddProductOptionAsync(productOptions);
                    await _shopifyRepo.AddVariantOptionValueAsync(variantOptionValues);
                    await _shopifyRepo.AddInventoryLevelsAsync(inventoryLevels);
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                var queueProduct = await _shopifyRepo.GetProductFromQueueAsync(productId.ToString());

                if (queueProduct == null || queueProduct.RetryCount > 10)
                {
                    await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                    {
                        Cursor = cursor,
                        LastError = ex.StackTrace ?? ex.Message,
                        ProductShopifyId = productId,
                        Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                        RetryCount = 0,
                        LastAttemptAt = null,
                        Filter = filter
                    });
                }

                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SaveShopifyProductDataAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }


        /// <summary>
        /// Get Failed Records to Reprocessed
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task RetryFailedPageAsync(ShopifyDataQueue item)
        {
            _logger.LogInformation("[ShopifyService] RetryFailedPageAsync called for item with cursor: {Cursor}, product ID: {ProductId}",
                item.Cursor, item.ProductShopifyId ?? "none");
            try
            {
                if (item.Cursor == "No Cursor Found")
                {
                    _logger.LogDebug("[ShopifyService] Retrying failed product import for ID: {ProductId}", item.ProductShopifyId);
                    var result = await ImportProductByIdAsync(Convert.ToInt64(item.ProductShopifyId));

                    if (result != null && (result.Contains("Successfully") || result.Contains("updated")))
                    {
                        _logger.LogInformation("[ShopifyService] Product retry successful for ID: {ProductId}, updating queue status", item.ProductShopifyId);
                        await _shopifyRepo.UpdateShopifyQueueAsync(item, true);
                        
                    }
                    else
                    {
                        _logger.LogWarning("[ShopifyService] Product retry failed for ID: {ProductId}, error: {Error}", item.ProductShopifyId, result);
                        item.LastError = result ?? "Unknown error occurred";
                        await _shopifyRepo.UpdateShopifyQueueAsync(item, false);
                        
                    }
                }
                else
                {
                    _logger.LogDebug("[ShopifyService] Retrying failed page with cursor: {Cursor}, filter: {Filter}", item.Cursor, item.Filter);
                    JsonElement? root = await GetProductsFromShopify(item.Cursor, item.Filter);
                    if (root == null)
                    {
                        _logger.LogWarning("[ShopifyService] Failed to get products for cursor: {Cursor}, JsonElement is null", item.Cursor);
                        item.LastError = "JsonElement is null";
                        await _shopifyRepo.UpdateShopifyQueueAsync(item, false);
                        return;
                    }

                    var products = new List<Product>();
                    var variants = new List<Variant>();
                    var collections = new List<Collection>();
                    var tags = new List<Tag>();
                    var productTags = new List<ProductTag>();
                    var productCollections = new List<ProductCollection>();
                    var options = new List<Option>();
                    var productOptions = new List<ProductOption>();
                    var inventoryLevels = new List<InventoryLevel>();
                    var optionValues = new List<OptionValue>();
                    var variantOptionValues = new List<VariantOptionValue>();

                    foreach (var edge in root.Value.GetProperty("edges").EnumerateArray())
                    {
                        var node = edge.GetProperty("node");
                        var shopifyProductId = node.GetProperty("id").GetString();

                        var dbProduct = await _shopifyRepo.GetProductByShopifyIdAsync(shopifyProductId);
                        if (dbProduct == null)
                        {
                            Product edgeProduct = await SetProductAsync(node, shopifyProductId);
                            products.Add(edgeProduct);

                            var optionDto = await SetOptionProductOptionAndOptionValueAsync(node, edgeProduct);
                            if (optionDto != null)
                            {
                                options.AddRange(optionDto.Options);
                                productOptions.AddRange(optionDto.ProductOptions);
                                optionValues.AddRange(optionDto.OptionValues);
                            }

                            if (node.TryGetProperty("variants", out var variantsNode))
                            {
                                var variantEdges = variantsNode.GetProperty("edges").EnumerateArray();
                                if (variantEdges.Any())
                                {
                                    foreach (var variantEdge in variantEdges)
                                    {
                                        var variantNode = variantEdge.GetProperty("node");
                                        Variant variant = await SetVariant(variantNode, edgeProduct);
                                        variants.Add(variant);
                                        var variantOptionValue = await SetVariantOptionValueAsync(optionValues, variantNode, variant);
                                        variantOptionValues.AddRange(variantOptionValue);
                                        if (variantNode.TryGetProperty("inventoryItem", out var inventoryItemNode) && inventoryItemNode.TryGetProperty("inventoryLevels", out var inventoryLevelsNode))
                                        {
                                            var inventoryLevelsData = await AddLocationAndSetInventoryLevelAsync(node, variant, inventoryLevelsNode);
                                            if (inventoryLevelsData != null && inventoryLevelsData.Any())
                                            {
                                                inventoryLevels.AddRange(inventoryLevelsData);
                                            }
                                        }
                                    }
                                }
                            }

                            var tagSetDto = await SetTagAndProductTagAsync(node, edgeProduct);
                            if (tagSetDto != null)
                            {
                                tags.AddRange(tagSetDto.Tags);
                                productTags.AddRange(tagSetDto.ProductTags);
                            }

                            if (node.TryGetProperty("collections", out var collectionsNode))
                            {
                                var collectionsDto = await SetCollectionAndProductCollectionAsync(collectionsNode, edgeProduct);
                                if (collectionsDto != null)
                                {
                                    productCollections.AddRange(collectionsDto.ProductCollections);
                                    collections.AddRange(collectionsDto.Collections);
                                }
                            }
                        }
                        else
                        {
                            await SetProductAndChildrenToUpdateAsync(node, dbProduct);
                            await UpdateProductAsync(dbProduct);
                        }
                    }

                    _logger.LogDebug("[ShopifyService] Saving retry data for cursor: {Cursor}", item.Cursor);
                    await SaveShopifyProductDataAsync(products, variants, options, productOptions, optionValues, variantOptionValues, tags, productTags, collections, productCollections, inventoryLevels);

                    _logger.LogDebug("[ShopifyService] Importing product images for retry cursor: {Cursor}", item.Cursor);
                    await ImportProductImagesAsync();

                    _logger.LogInformation("[ShopifyService] Page retry successful for cursor: {Cursor}, updating queue status", item.Cursor);
                    await _shopifyRepo.UpdateShopifyQueueAsync(item, true);
                    
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in RetryFailedPageAsync for cursor: {Cursor}: {Message}", item.Cursor, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "RetryFailedPageAsync", 1, ex.Message, ex.ToString());
                item.LastError = ex.Message;
                await _shopifyRepo.UpdateShopifyQueueAsync(item, false);
            }
        }

        /// <summary>
        /// Get Failed Record from Queue
        /// </summary>
        /// <returns></returns>
        public async Task<List<ShopifyDataQueue>> GetFailedQueueAsync()
        {
            _logger.LogDebug("[ShopifyService] GetFailedQueueAsync called");
            try
            {
                var failedItems = await _shopifyRepo.GetFailedQueueAsync();
                _logger.LogDebug("[ShopifyService] Retrieved {Count} failed queue items", failedItems?.Count ?? 0);
                return failedItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in GetFailedQueueAsync: {Message}", ex.Message);
                throw;
            }
        }

        #region Setters
        /// <summary>
        /// Adds locations and sets inventory levels for a variant.
        /// </summary>
        /// <param name="node">The JSON node containing inventory levels.</param>
        /// <param name="variant">The variant entity.</param>
        /// <param name="inventoryLevelsNode">The JSON node containing inventory level details.</param>
        /// <returns>A list of InventoryLevel entities.</returns>
        public async Task<List<InventoryLevel>> AddLocationAndSetInventoryLevelAsync(JsonElement node, Variant variant, JsonElement inventoryLevelsNode)
        {
            _logger.LogDebug("[ShopifyService] AddLocationAndSetInventoryLevelAsync called for variant ID: {VariantId}", variant.Id);
            try
            {
                List<InventoryLevel> inventoryLevels = new List<InventoryLevel>();
                List<Location> locations = new List<Location>();
                var locationShopifyIds = inventoryLevelsNode.GetProperty("edges")
                                        .EnumerateArray()
                                        .Select(edge => edge.GetProperty("node").GetProperty("location").GetProperty("id").GetString().Split('/').Last())
                                        .ToList();

                _logger.LogDebug("[ShopifyService] Found {Count} location IDs for variant {VariantId}", locationShopifyIds.Count, variant.Id);

                var dbLocations = await _shopifyRepo.GetLocationsByShopifyIdsAsync(locationShopifyIds);

                foreach (var invEdge in inventoryLevelsNode.GetProperty("edges").EnumerateArray())
                {
                    var invNode = invEdge.GetProperty("node");

                    var locationNode = invNode.GetProperty("location");
                    var addressNode = locationNode.GetProperty("address");
                    var locationShopifyId = locationNode.GetProperty("id").GetString().Split('/').Last();
                    var dbLocation = dbLocations.Where(x => x.ShopifyId == locationShopifyId).FirstOrDefault();
                    if (dbLocation == null)
                    {
                        dbLocation = new Location
                        {
                            ShopifyId = locationShopifyId,
                            Name = locationNode.GetProperty("name").GetString(),
                            City = addressNode.TryGetProperty("city", out var city) ? city.GetString() : null,
                            Province = addressNode.TryGetProperty("province", out var province) ? province.GetString() : null,
                            Country = addressNode.TryGetProperty("country", out var country) ? country.GetString() : null,
                            Zip = addressNode.TryGetProperty("zip", out var zip) ? zip.GetString() : null,
                        };
                        locations.Add(dbLocation);
                    }

                    int availableQuantity = 0;
                    if (invNode.TryGetProperty("quantities", out var quantitiesElement))
                    {
                        foreach (var quantityItem in quantitiesElement.EnumerateArray())
                        {
                            var name = quantityItem.GetProperty("name").GetString();
                            if (name == "available" && quantityItem.TryGetProperty("quantity", out var quantityValue))
                            {
                                availableQuantity = quantityValue.GetInt32();
                                break;
                            }
                        }
                    }
                    var inventoryLevel = new InventoryLevel
                    {
                        UpdatedAt = DateTime.SpecifyKind(DateTime.Parse(invNode.GetProperty("updatedAt").GetString()), DateTimeKind.Utc),
                        Variant = variant,
                        Location = dbLocation,
                        Available = availableQuantity
                    };
                    inventoryLevels.Add(inventoryLevel);
                }

                if (locations.Any())
                {
                    _logger.LogDebug("[ShopifyService] Adding {Count} new locations for variant {VariantId}", locations.Count, variant.Id);
                    await _shopifyRepo.AddLocationsAsync(locations);
                }

                _logger.LogDebug("[ShopifyService] Created {Count} inventory levels for variant {VariantId}", inventoryLevels.Count, variant.Id);
                return inventoryLevels;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in AddLocationAndSetInventoryLevelAsync for variant {VariantId}: {Message}", variant.Id, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "AddLocationAndSetInventoryLevelAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<Variant> SetVariant(JsonElement varNode, Product product)
        {
            _logger.LogDebug("[ShopifyService] SetVariant called for product ID: {ProductId}", product.Id);
            try
            {
                var variant = new Variant
                {
                    ShopifyId = varNode.GetProperty("id").GetString(),
                    Title = varNode.GetProperty("title").GetString(),
                    SKU = varNode.GetProperty("sku").GetString(),
                    CompareAtPrice = varNode.TryGetProperty("compareAtPrice", out var cap) ? cap.GetString() : null,
                    Barcode = varNode.GetProperty("barcode").GetString(),
                    //Weight = varNode.TryGetProperty("weight", out var weightElement) &&
                    //weightElement.ValueKind == JsonValueKind.Number &&
                    //weightElement.TryGetSingle(out var parsedWeight)
                    //? parsedWeight
                    //: 0f,
                    Price = varNode.GetProperty("price").GetString(),
                   // WeightUnit = varNode.GetProperty("weightUnit").GetString(), // Map as needed
                    CreatedAt = DateTime.SpecifyKind(DateTime.Parse(varNode.GetProperty("createdAt").GetString()), DateTimeKind.Utc),
                    UpdatedAt = DateTime.SpecifyKind(DateTime.Parse(varNode.GetProperty("updatedAt").GetString()), DateTimeKind.Utc),
                    VariantPrices = new List<VariantPrice>(),
                    OemVariants = new List<OemVariant>(),
                    Product = product
                };

                if (varNode.TryGetProperty("metafields", out var pricesNode))
                {
                    foreach (var priceEdge in pricesNode.GetProperty("edges").EnumerateArray())
                    {
                        var node = priceEdge.GetProperty("node");

                        if (!string.IsNullOrEmpty(node.GetProperty("namespace").GetString()) && node.GetProperty("namespace").GetString() == variPriceNameSpace)
                        {
                            var value = node.GetProperty("value").GetString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                var variantPrices = await SetVariantPrices(value);
                                variant.VariantPrices.AddRange(variantPrices);
                            }
                        }

                        if (!string.IsNullOrEmpty(node.GetProperty("namespace").GetString()) && node.GetProperty("namespace").GetString() == customNameSpace)
                        {
                            var value = node.GetProperty("value").GetString();  

                            if (node.GetProperty("key").GetString() == oemKey)
                            {
                                var oem = await _shopifyRepo.GetOrCreateOEMByNameAsync(value);
                                variant.OEM = oem;
                                variant.OEMMetaField = value;
                            }
                            if (node.GetProperty("key").GetString() == oemcomplementairesKey)
                            {
                                var oemVariant = await SetOEMVariant(value);
                                variant.OemVariants.AddRange(oemVariant);
                            }


                        }
                    }
                }
                _logger.LogDebug("[ShopifyService] Successfully created variant for product ID: {ProductId}", product.Id);
                return variant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in SetVariant for product {ProductId}: {Message}", product.Id, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetVariant", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<List<OemVariant>> SetOEMVariant(string value)
        {
            try
            {
                var oemcomplementairValues = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(value);
                List<OemVariant> oemVariants = new List<OemVariant>();
                foreach (var oemcomplementair in oemcomplementairValues)
                {
                    var oemVariant = new OemVariant()
                    {
                        OEM = new OEM() { Name = oemcomplementair },
                    };
                    oemVariants.Add(oemVariant);
                }

                return oemVariants;

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetOEMVariant", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<List<VariantPrice>> SetVariantPrices(string value)
        {
            try
            {
                var customPrices = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(value);

                var locationShopifyIds = customPrices.Select(x => x.Key).ToList();
                var variantPrices = new List<VariantPrice>();
                var dbLocations = await _shopifyRepo.GetLocationsByShopifyIdsAsync(locationShopifyIds);
                foreach (var customPrice in customPrices)
                {
                    var location = dbLocations.FirstOrDefault(l => l.ShopifyId == customPrice.Key);
                    if (location != null)
                    {
                        var variantPrice = new VariantPrice()
                        {
                            Location = location,
                            Price = customPrice.Value,
                        };
                        variantPrices.Add(variantPrice);
                    }
                }

                return variantPrices;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetVariantPrices", 1, ex.Message, ex.ToString());
                throw;
            }

        }

        private async Task<OptionSetDTO> SetOptionProductOptionAndOptionValueAsync(JsonElement node, Product edgeProduct)
        {
            try
            {
                List<OptionValue> optionValues = new List<OptionValue>();
                List<ProductOption> productOptions = new List<ProductOption>();
                var options = new List<Option>();
                if (node.TryGetProperty("options", out var optionsNode))
                {
                    foreach (var optionNode in optionsNode.EnumerateArray())
                    {
                        //var optionId = optionNode.GetProperty("id").GetString();
                        var optionName = optionNode.GetProperty("name").GetString();
                        var position = optionNode.GetProperty("position").GetInt32();
                        var option = options.FirstOrDefault(o => o.Name == optionName);
                        if (option == null)
                        {
                            option = new Option
                            {
                                Name = optionName
                            };
                            options.Add(option);
                        }
                        productOptions.Add(new ProductOption
                        {
                            Product = edgeProduct,
                            Option = option,
                            Position = position

                        });
                        foreach (var tagValue in optionNode.GetProperty("values").EnumerateArray())
                        {
                            var optionValue = new OptionValue
                            {
                                Option = option,
                                Value = tagValue.GetString()
                            };
                            optionValues.Add(optionValue);
                        }
                    }

                }
                OptionSetDTO optionSetDTO = new OptionSetDTO
                {
                    Options = options,
                    ProductOptions = productOptions,
                    OptionValues = optionValues
                };

                return optionSetDTO;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetOptionProductOptionAndOptionValueAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<TagSetDTO> SetTagAndProductTagAsync(JsonElement node, Product edgeProduct)
        {
            TagSetDTO tagSetDTO = new TagSetDTO();
            List<ProductTag> productTags = new List<ProductTag>();
            var tags = new List<Tag>();
            try
            {
                foreach (var tagValue in node.GetProperty("tags").EnumerateArray())
                {
                    var tagTitle = tagValue.GetString();
                    var tag = tags.FirstOrDefault(t => t.Title == tagTitle);
                    if (tag == null)
                    {
                        tag = new Tag
                        {
                            Title = tagTitle,
                            Title_en = tagTitle,
                            CreatedAt = DateTime.SpecifyKind(DateTime.Parse(edgeProduct.CreatedAt.ToString()), DateTimeKind.Utc),
                            UpdatedAt = DateTime.SpecifyKind(DateTime.Parse(edgeProduct.UpdatedAt.ToString()), DateTimeKind.Utc)
                        };
                        tags.Add(tag);
                    }
                    productTags.Add(new ProductTag
                    {
                        Product = edgeProduct,
                        Tag = tag
                    });
                }
                tagSetDTO.ProductTags = productTags;
                tagSetDTO.Tags = tags;

                return tagSetDTO;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetTagAndProductTagAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<Product> SetProductAsync(JsonElement node, string? shopifyProductId)
        {
            var productId = node.GetProperty("id").GetString().Split('/').Last();
            decimal minCompareAtPrice = 0;
            decimal maxCompareAtPrice = 0;
            try
            {
                if (node.TryGetProperty("compareAtPriceRange", out var priceRangeNode))
                {
                    if (priceRangeNode.ValueKind != JsonValueKind.Null)
                    {
                        if (priceRangeNode.TryGetProperty("minVariantCompareAtPrice", out var minNode) &&
                            minNode.TryGetProperty("amount", out var minAmountNode) &&
                            decimal.TryParse(minAmountNode.GetString(), out var minAmount))
                        {
                            minCompareAtPrice = minAmount;
                        }
                        if (priceRangeNode.TryGetProperty("maxVariantCompareAtPrice", out var maxNode) &&
                            maxNode.TryGetProperty("amount", out var maxAmountNode) &&
                            decimal.TryParse(maxAmountNode.GetString(), out var maxAmount))
                        {
                            maxCompareAtPrice = maxAmount;
                        }
                    }
                }
                var vendorName = node.GetProperty("vendor").GetString();
                var vendor = await _shopifyRepo.GetOrCreateVendorByNameAsync(vendorName);
                
                ProductType productType = null;
                if (node.TryGetProperty("productType", out var productTypeProperty) && 
                    productTypeProperty.ValueKind != JsonValueKind.Null &&
                    !string.IsNullOrWhiteSpace(productTypeProperty.GetString()))
                {
                    var productTypeName = productTypeProperty.GetString();
                    productType = await _shopifyRepo.GetOrCreateProductTypeByNameAsync(productTypeName);
                }

                var tags = node.GetProperty("tags").EnumerateArray().Select(x => x.GetString()).ToList();
                var edgeProduct = new Product
                {
                    ShopifyId = shopifyProductId,
                    Title = node.GetProperty("title").GetString(),
                    DescriptionHtml = node.GetProperty("descriptionHtml").GetString(),
                    Status = node.GetProperty("status").GetString(),
                    Handle = node.GetProperty("handle").GetString(), // Shopify handle is string, map as needed
                    CreatedAt = DateTime.SpecifyKind(DateTime.Parse(node.GetProperty("createdAt").GetString()), DateTimeKind.Utc),
                    UpdatedAt = DateTime.SpecifyKind(DateTime.Parse(node.GetProperty("updatedAt").GetString()), DateTimeKind.Utc),
                    VendorId = vendor.Id,
                    ProductTypeId = productType?.Id,
                    LocalCreatedAt = DateTime.UtcNow,
                    CompareAtPriceMin = minCompareAtPrice,
                    CompareAtPriceMax = maxCompareAtPrice,
                    Is_Piece = tags.Contains("piece"),
                    Exact_Fit = tags.Contains("partsfinder")

                };
                return edgeProduct;
            }
            catch (Exception ex)
            {
                var queueProduct = await _shopifyRepo.GetProductFromQueueAsync(productId.ToString());

                if (queueProduct == null || queueProduct.RetryCount > 10)
                {
                    await _shopifyRepo.AddShopifyQueueAsync(new ShopifyDataQueue()
                    {
                        Cursor = "No Cursor Found",
                        LastError = ex.StackTrace ?? ex.Message,
                        ProductShopifyId = productId,
                        Status = ShopifySync_DataAccessLayer.Enum.QueueStatus.Failed,
                        RetryCount = 0,
                        LastAttemptAt = null
                    });
                }
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetProductAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<List<VariantOptionValue>> SetVariantOptionValueAsync(List<OptionValue> optionValues, JsonElement varNode, Variant variant)
        {
            try
            {
                List<VariantOptionValue> variantOptionValues = new List<VariantOptionValue>();
                foreach (var nodeVariantOptionValue in varNode.GetProperty("selectedOptions").EnumerateArray())
                {
                    var value = nodeVariantOptionValue.GetProperty("value").GetString();
                    var optionValue = optionValues.FirstOrDefault(o => o.Value == value);
                    var variantOptionValue = new VariantOptionValue
                    {
                        OptionValue = optionValue,
                        Variant = variant,
                        Position = value

                    };
                    variantOptionValues.Add(variantOptionValue);
                }
                return variantOptionValues;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetVariantOptionValueAsync", 1, ex.Message, ex.ToString());
                throw;
            }

        }

        private async Task<CollectionSetDTO> SetCollectionAndProductCollectionAsync(JsonElement collectionsNode, Product edgeProduct)
        {
            List<ProductCollection> productCollections = new List<ProductCollection>();
            CollectionSetDTO collectionSetDTO = new CollectionSetDTO();
            var newCollections = new List<Collection>();

            var nodes = collectionsNode.GetProperty("edges").EnumerateArray().Select(x => x.GetProperty("node")).ToList();
            var collectionShopifyIds = nodes.Select(x => x.GetProperty("id").GetString()).ToList();

            var product = await _shopifyRepo.GetProductsByShopifyIds(edgeProduct.ShopifyId);

            try
            {
                foreach (var collEdge in collectionsNode.GetProperty("edges").EnumerateArray())
                {
                    var collNode = collEdge.GetProperty("node");
                    var shopifyCollectionId = collNode.GetProperty("id").GetString();
                    var newCollection = new Collection();

                    if (product == null || (!product.Any(x => x.ProductCollections.Any(y => y.Collection.ShopifyId == shopifyCollectionId))))
                    {
                        newCollection = await _shopifyRepo.GetCollectionByShopifyId(shopifyCollectionId);
                        if (newCollection == null)
                        {
                            newCollection = new Collection
                            {
                                ShopifyId = shopifyCollectionId,
                                Title = collNode.GetProperty("title").GetString(),
                                Description = collNode.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                                Image = collNode.TryGetProperty("image", out var img) &&
                       img.ValueKind == JsonValueKind.Object &&
                       img.TryGetProperty("src", out var src)
                       ? src.GetString() : null
                            };
                        }

                        newCollections.Add(newCollection);
                        productCollections.Add(new ProductCollection
                        {
                            Product = edgeProduct,
                            Collection = newCollection
                        });
                    }
                    else
                    {
                        var collection = product?.FirstOrDefault()?.ProductCollections;
                        if (collection != null && collection.Any(x => x.Collection.ShopifyId == shopifyCollectionId))
                        {
                            productCollections.Add(new ProductCollection
                            {
                                Product = edgeProduct,
                                Collection = collection.Where(x => x.Collection.ShopifyId == shopifyCollectionId)?.FirstOrDefault()?.Collection
                            });
                        }
                    }
                }

                collectionSetDTO.Collections = newCollections.ToList();
                collectionSetDTO.ProductCollections = productCollections;

                return collectionSetDTO;
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetCollectionAndProductCollectionAsync", 1, ex.Message, ex.ToString());
                throw;
            }

        }
        #endregion

        /// <summary>
        /// Updates an existing product and all its related entities from Shopify data with full synchronization.
        /// </summary>
        /// <param name="node">The JSON node containing the product data from Shopify.</param>
        /// <param name="existingProduct">The existing product entity to update.</param>
        /// <returns>The updated product entity.</returns>
        public async Task<Product> UpdateProductAsync(Product existingProduct)
        {
            _logger.LogDebug("[ShopifyService] UpdateProductAsync called for product ID: {ProductId}", existingProduct.Id);
            try
            {
                await _shopifyRepo.UpdateProductAsync(existingProduct);
                _logger.LogDebug("[ShopifyService] Successfully updated product ID: {ProductId}", existingProduct.Id);

                return existingProduct;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in UpdateProductAsync for product {ProductId}: {Message}", existingProduct.Id, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateProductAndChildrenAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Adds product images to the database.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public async Task AddProductImagesAsync(JsonElement productNode)
        {
            var productImages = new List<ProductImage>();
            var productShopifyId = productNode.GetProperty("id").GetString();
            _logger.LogDebug("[ShopifyService] AddProductImagesAsync called for product Shopify ID: {ShopifyId}", productShopifyId);
            if (productNode.TryGetProperty("images", out var imagesNode))
            {
                foreach (var imageEdge in imagesNode.GetProperty("edges").EnumerateArray())
                {
                    var imageNode = imageEdge.GetProperty("node");
                    var product = await _shopifyRepo.GetProductByShopifyIdAsync(productShopifyId);

                    if (product == null)
                    {
                        continue;
                    }
                    else
                    {
                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageSrc = imageNode.GetProperty("src").GetString(),
                            ImageShopifyId = imageNode.GetProperty("id").GetString(),
                            LocalCreatedAt = DateTime.UtcNow,

                        };
                        productImages.Add(productImage);
                    }
                }
            }
            if (productImages.Any())
            {
                _logger.LogInformation("[ShopifyService] Adding {Count} product images for product {ShopifyId}", productImages.Count, productShopifyId);
                await _shopifyRepo.AddProductImagesAsync(productImages);
                _logger.LogInformation("[ShopifyService] Successfully added {Count} product images for product {ShopifyId}", productImages.Count, productShopifyId);
            }
            else
            {
                _logger.LogDebug("[ShopifyService] No product images to add for product {ShopifyId}", productShopifyId);
            }
        }

        private async Task SetProductAndChildrenToUpdateAsync(JsonElement node, Product existingProduct)
        {
            _logger.LogDebug("[ShopifyService] SetProductAndChildrenToUpdateAsync called for product ID: {ProductId}", existingProduct.Id);
            try
            {
                _logger.LogDebug("[ShopifyService] Updating basic product information for ID: {ProductId}", existingProduct.Id);
                await SetProductToUpdateAsync(node, existingProduct);

                await SynchronizeOptionsAsync(node, existingProduct);

                if (node.TryGetProperty("variants", out var variantsNode))
                {
                    await SynchronizeVariantsAsync(variantsNode, existingProduct);
                }

                await SynchronizeTagsAsync(node, existingProduct);

                if (node.TryGetProperty("collections", out var collectionsNode))
                {
                    _logger.LogDebug("[ShopifyService] Synchronizing collections for product ID: {ProductId}", existingProduct.Id);
                    await SynchronizeCollectionsAsync(collectionsNode, existingProduct);
                }

                _logger.LogDebug("[ShopifyService] Successfully completed product and children update for ID: {ProductId}", existingProduct.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in SetProductAndChildrenToUpdateAsync for product {ProductId}: {Message}", existingProduct.Id, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SetProductAndChildrenToUpdateAsync", 1, ex.Message, ex.ToString());
                throw;
            }

        }

        private async Task SetProductToUpdateAsync(JsonElement node, Product existingProduct)
        {
            _logger.LogDebug("[ShopifyService] SetProductToUpdateAsync called for product ID: {ProductId}", existingProduct.Id);
            try
            {
                decimal minCompareAtPrice = 0;
                decimal maxCompareAtPrice = 0;

                if (node.TryGetProperty("compareAtPriceRange", out var priceRangeNode))
                {
                    if (priceRangeNode.ValueKind != JsonValueKind.Null)
                    {
                        if (priceRangeNode.TryGetProperty("minVariantCompareAtPrice", out var minNode) &&
                        minNode.TryGetProperty("amount", out var minAmountNode) &&
                        decimal.TryParse(minAmountNode.GetString(), out var minAmount))
                        {
                            minCompareAtPrice = minAmount;
                        }
                        if (priceRangeNode.TryGetProperty("maxVariantCompareAtPrice", out var maxNode) &&
                            maxNode.TryGetProperty("amount", out var maxAmountNode) &&
                            decimal.TryParse(maxAmountNode.GetString(), out var maxAmount))
                        {
                            maxCompareAtPrice = maxAmount;
                        }
                    }
                }

                var vendorName = node.GetProperty("vendor").GetString();
                var vendor = await _shopifyRepo.GetOrCreateVendorByNameAsync(vendorName);

                ProductType productType = null;
                if (node.TryGetProperty("productType", out var productTypeProperty) && 
                    productTypeProperty.ValueKind != JsonValueKind.Null &&
                    !string.IsNullOrWhiteSpace(productTypeProperty.GetString()))
                {
                    var productTypeName = productTypeProperty.GetString();
                    productType = await _shopifyRepo.GetOrCreateProductTypeByNameAsync(productTypeName);
                }

                var tags = node.GetProperty("tags").EnumerateArray().Select(x => x.GetString()).ToList();
                existingProduct.Title = node.GetProperty("title").GetString();
                existingProduct.DescriptionHtml = node.GetProperty("descriptionHtml").GetString();
                existingProduct.Status = node.GetProperty("status").GetString();
                existingProduct.Handle = node.GetProperty("handle").GetString();
                existingProduct.UpdatedAt = DateTime.SpecifyKind(DateTime.Parse(node.GetProperty("updatedAt").GetString()), DateTimeKind.Utc);
                existingProduct.LocalUpdatedAt = DateTime.UtcNow;
                existingProduct.VendorId = vendor.Id;
                existingProduct.ProductTypeId = productType?.Id;
                existingProduct.CompareAtPriceMin = minCompareAtPrice;
                existingProduct.CompareAtPriceMax = maxCompareAtPrice;
                existingProduct.Is_Piece = tags.Contains("piece");
                existingProduct.Exact_Fit = tags.Contains("partsfinder");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateProductBasicInfoAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }


        private async Task UpdateVariantAsync(JsonElement variantNode, Variant existingVariant)
        {
            try
            {
                existingVariant.Title = variantNode.GetProperty("title").GetString();
                existingVariant.SKU = variantNode.GetProperty("sku").GetString();
                existingVariant.CompareAtPrice = variantNode.TryGetProperty("compareAtPrice", out var cap) ? cap.GetString() : null;
                existingVariant.Barcode = variantNode.GetProperty("barcode").GetString();
                //existingVariant.Weight = variantNode.TryGetProperty("weight", out var weightElement) &&
                //    weightElement.ValueKind == JsonValueKind.Number &&
                //    weightElement.TryGetSingle(out var parsedWeight)
                //    ? parsedWeight
                //    : 0f;
                existingVariant.Price = variantNode.GetProperty("price").GetString();
                //existingVariant.WeightUnit = variantNode.GetProperty("weightUnit").GetString();
                existingVariant.UpdatedAt = DateTime.SpecifyKind(DateTime.Parse(variantNode.GetProperty("updatedAt").GetString()), DateTimeKind.Utc);

                // Handle variant.OEM from metafields
                if (variantNode.TryGetProperty("metafields", out var metafieldsNode))
                {
                    foreach (var metafieldEdge in metafieldsNode.GetProperty("edges").EnumerateArray())
                    {
                        var metafieldNode = metafieldEdge.GetProperty("node");
                        if (!string.IsNullOrEmpty(metafieldNode.GetProperty("namespace").GetString()) &&
                            metafieldNode.GetProperty("namespace").GetString() == customNameSpace &&
                            metafieldNode.GetProperty("key").GetString() == oemKey)
                        {
                            var value = metafieldNode.GetProperty("value").GetString();
                            var oem = await _shopifyRepo.GetOrCreateOEMByNameAsync(value);

                            if (existingVariant.OEM !=null && existingVariant.OEM.Name != oem.Name)
                            {
                                existingVariant.OEM = oem;
                            }
                            else if (existingVariant.OEM == null)
                            {
                                existingVariant.OEM = oem;
                            }
                            existingVariant.OEMMetaField = value;
                            break;
                        }
                    }
                }

                await _shopifyRepo.UpdateVariantAsync(existingVariant);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeOptionsAsync(JsonElement node, Product existingProduct)
        {
            try
            {
                var optionDto = await SetOptionProductOptionAndOptionValueAsync(node, existingProduct);
                if (optionDto == null) return;

                var existingProductOptions = await _shopifyRepo.GetProductOptionsByProductIdAsync(existingProduct.Id) ?? new List<ProductOption>();

                var existingOptionIds = existingProductOptions.Select(po => po.OptionId).ToHashSet();
                var existingOptions = await _shopifyRepo.GetOptionsByIdsAsync(existingOptionIds);

                var existingOptionValues = await _shopifyRepo.GetOptionValuesByProductIdAsync(existingProduct.Id) ?? new List<OptionValue>();

                var shopifyOptionNames = optionDto.Options.Select(o => o.Name).ToHashSet();
                var existingOptionNames = existingOptions.Select(o => o.Name).ToHashSet();

                var optionsToDelete = existingOptions.Where(o => !shopifyOptionNames.Contains(o.Name)).ToList();
                var optionsToUpdate = existingOptions.Where(o => shopifyOptionNames.Contains(o.Name)).ToList();
                var optionsToAdd = optionDto.Options.Where(o => !existingOptionNames.Contains(o.Name)).ToList();

                if (optionsToDelete.Any())
                {
                    var productOptionsToDelete = existingProductOptions.Where(po =>
                        optionsToDelete.Any(o => o.Id == po.OptionId)).ToList();
                    await _shopifyRepo.DeleteProductOptionAsync(productOptionsToDelete);

                    var optionValuesToDelete = existingOptionValues.Where(ov =>
                        optionsToDelete.Any(o => o.Id == ov.OptionId)).ToList();
                    await _shopifyRepo.DeleteOptionValueAsync(optionValuesToDelete);
                }

                foreach (var optionToUpdate in optionsToUpdate)
                {
                    var shopifyOption = optionDto.Options.FirstOrDefault(o => o.Name == optionToUpdate.Name);
                    if (shopifyOption != null)
                    {
                        if (optionToUpdate.Name != shopifyOption.Name)
                        {
                            optionToUpdate.Name = shopifyOption.Name;
                            await _shopifyRepo.UpdateOptionAsync(optionToUpdate);
                        }
                    }
                }

                if (optionsToAdd.Any())
                {
                    await _shopifyRepo.AddOptionsAsync(optionsToAdd);
                }

                var shopifyOptionValuePairs = optionDto.OptionValues.Select(ov => new { ov.Option?.Name, ov.Value }).ToHashSet();
                var existingOptionValuePairs = existingOptionValues.Select(ov => new { ov.Option?.Name, ov.Value }).ToHashSet();

                var optionValuesToDeleteFromShopify = existingOptionValues.Where(ov =>
                    ov.Option?.Name != null && ov.Value != null &&
                    !shopifyOptionValuePairs.Any(sov => sov.Name == ov.Option.Name && sov.Value == ov.Value)).ToList();

                var optionValuesToUpdate = existingOptionValues.Where(ov =>
                    ov.Option?.Name != null && ov.Value != null &&
                    shopifyOptionValuePairs.Any(sov => sov.Name == ov.Option.Name && sov.Value == ov.Value)).ToList();

                var optionValuesToAdd = optionDto.OptionValues.Where(ov =>
                    ov.Option?.Name != null && ov.Value != null &&
                    !existingOptionValuePairs.Any(eov => eov.Name == ov.Option.Name && eov.Value == ov.Value)).ToList();


                foreach (var optionValueToUpdate in optionValuesToUpdate)
                {
                    var shopifyOptionValue = optionDto.OptionValues.FirstOrDefault(ov =>
                        ov.Option?.Name == optionValueToUpdate.Option?.Name && ov.Value == optionValueToUpdate.Value);
                    if (shopifyOptionValue != null)
                    {
                        if (optionValueToUpdate.Value != shopifyOptionValue.Value)
                        {
                            optionValueToUpdate.Value = shopifyOptionValue.Value;
                            await _shopifyRepo.UpdateOptionValueAsync(optionValueToUpdate);
                        }
                    }
                }

                if (optionValuesToAdd.Any())
                {
                    await _shopifyRepo.AddOptionValuesAsync(optionValuesToAdd);
                }

                var shopifyProductOptionPairs = optionDto.ProductOptions.Select(po => po.Option?.Name).ToHashSet();
                var existingProductOptionPairs = existingProductOptions.Select(po => po.Option?.Name).ToHashSet();

                var productOptionsToDeleteFromShopify = existingProductOptions.Where(po =>
                    po.Option?.Name != null && !shopifyProductOptionPairs.Contains(po.Option.Name)).ToList();

                var productOptionsToUpdate = existingProductOptions.Where(po =>
                    po.Option?.Name != null && shopifyProductOptionPairs.Contains(po.Option.Name)).ToList();

                var productOptionsToAdd = optionDto.ProductOptions.Where(po =>
                    po.Option?.Name != null && !existingProductOptionPairs.Contains(po.Option.Name)).ToList();


                foreach (var productOptionToUpdate in productOptionsToUpdate)
                {
                    var shopifyProductOption = optionDto.ProductOptions.FirstOrDefault(po =>
                        po.Option?.Name == productOptionToUpdate.Option?.Name);
                    if (shopifyProductOption != null)
                    {
                        if (productOptionToUpdate.Position != shopifyProductOption.Position)
                        {
                            productOptionToUpdate.Position = shopifyProductOption.Position;
                            await _shopifyRepo.UpdateProductOptionAsync(productOptionToUpdate);
                        }
                    }
                }

                if (productOptionsToAdd.Any())
                {
                    await _shopifyRepo.AddProductOptionAsync(productOptionsToAdd);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeOptionsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeVariantsAsync(JsonElement variantsNode, Product existingProduct)
        {
            try
            {
                var variantEdges = variantsNode.GetProperty("edges").EnumerateArray();
                var shopifyVariants = new List<JsonElement>();

                foreach (var variantEdge in variantEdges)
                {
                    shopifyVariants.Add(variantEdge.GetProperty("node"));
                }

                // var existingVariants = existingProduct.Variants?.ToList() ?? new List<Variant>();
                var existingVariants = await _shopifyRepo.GetVariantsByProductIdAsync(existingProduct.Id) ?? new List<Variant>();

                var shopifyVariantIds = shopifyVariants.Select(v => v.GetProperty("id").GetString()).ToHashSet();
                var existingVariantIds = existingVariants.Select(v => v.ShopifyId).Where(id => id != null).ToHashSet();

                var variantsToDelete = existingVariants.Where(v =>
                    v.ShopifyId != null && !shopifyVariantIds.Contains(v.ShopifyId)).ToList();

                if (variantsToDelete.Any())
                {
                    _logger.LogInformation("[ShopifyService] Deleting {Count} variants that no longer exist in Shopify", variantsToDelete.Count);
                    await _shopifyRepo.DeleteVariantAsync(variantsToDelete);
                }

                List<Variant> newVariantsToAdd = new List<Variant>();
                var newVariantNodes = new Dictionary<string, JsonElement>(); // Track JSON nodes for new variants
                
                foreach (var variantNode in shopifyVariants)
                {
                    var variantShopifyId = variantNode.GetProperty("id").GetString();
                    var existingVariant = existingVariants.FirstOrDefault(v => v.ShopifyId == variantShopifyId);

                    if (existingVariant != null)
                    {
                        await SynchronizeVariantAsync(variantNode, existingVariant);
                    }
                    else
                    {
                        // Create new variant (but don't add related data yet - variant has no ID)
                        _logger.LogDebug("[ShopifyService] Creating new variant with Shopify ID: {ShopifyId}", variantShopifyId);
                        var newVariant = await SetVariant(variantNode, existingProduct);
                        newVariantsToAdd.Add(newVariant);
                        newVariantNodes[variantShopifyId] = variantNode; // Store the JSON node for later
                    }
                }
                
                // Save new variants to database - they will get IDs assigned
                if (newVariantsToAdd.Any())
                {
                    _logger.LogInformation("[ShopifyService] Adding {Count} new variants to database", newVariantsToAdd.Count);
                    await _shopifyRepo.AddVariants(newVariantsToAdd);
                    
                    // Now add related data for new variants (they have IDs now)
                    foreach (var newVariant in newVariantsToAdd)
                    {
                        if (newVariantNodes.TryGetValue(newVariant.ShopifyId, out var variantNode))
                        {
                            _logger.LogDebug("[ShopifyService] Adding related data for new variant ID: {VariantId}, Shopify ID: {ShopifyId}", 
                                newVariant.Id, newVariant.ShopifyId);
                            await AddVariantRelatedDataAsync(variantNode, newVariant);
                        }
                    }
                    
                    _logger.LogInformation("[ShopifyService] Successfully added {Count} new variants with related data", newVariantsToAdd.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error in SynchronizeVariantsAsync for product {ProductId}: {Message}", 
                    existingProduct.Id, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeVariantsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeVariantAsync(JsonElement variantNode, Variant existingVariant)
        {
            try
            {
                await UpdateVariantAsync(variantNode, existingVariant);

                await SynchronizeVariantOptionValuesAsync(variantNode, existingVariant);

                await SynchronizeInventoryLevelsAsync(variantNode, existingVariant);

                await SynchronizeVariantPricesAsync(variantNode, existingVariant);

                await SynchronizeOemVariantsAsync(variantNode, existingVariant);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeVariantAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task AddVariantRelatedDataAsync(JsonElement variantNode, Variant variant)
        {
            try
            {
                var optionValues = await GetOptionValuesForProduct(variant.ProductId);
                var variantOptionValues = await SetVariantOptionValueAsync(optionValues, variantNode, variant);
                await _shopifyRepo.AddVariantOptionValueAsync(variantOptionValues);

                if (variantNode.TryGetProperty("inventoryItem", out var inventoryItemNode) &&
                    inventoryItemNode.TryGetProperty("inventoryLevels", out var inventoryLevelsNode))
                {
                    var inventoryLevelsData = await AddLocationAndSetInventoryLevelAsync(variantNode, variant, inventoryLevelsNode);
                    if (inventoryLevelsData != null && inventoryLevelsData.Any())
                    {
                        await _shopifyRepo.AddInventoryLevelsAsync(inventoryLevelsData);
                    }
                }

                //if (variantNode.TryGetProperty("metafields", out var metafieldsNode))
                //{
                //    await ProcessVariantMetafieldsAsync(metafieldsNode, variant);
                //}
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "AddVariantRelatedDataAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<List<OptionValue>> GetOptionValuesForProduct(int productId)
        {
            try
            {
                return await _shopifyRepo.GetOptionValuesByProductIdAsync(productId);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOptionValuesForProduct", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeVariantOptionValuesAsync(JsonElement variantNode, Variant existingVariant)
        {
            try
            {
                var optionValues = await GetOptionValuesForProduct(existingVariant.ProductId);
                var newVariantOptionValues = await SetVariantOptionValueAsync(optionValues, variantNode, existingVariant);

                var existingVariantOptionValues = await _shopifyRepo.GetVariantOptionValuesByVariantIdAsync(existingVariant.Id);

                // Create sets for comparison using both option name and value for proper matching
                var newVariantOptionValuePairs = newVariantOptionValues.Select(vov => new
                {
                    OptionName = vov.OptionValue?.Option?.Name,
                    OptionValue = vov.OptionValue?.Value
                }).Where(p => p.OptionName != null && p.OptionValue != null).ToHashSet();

                var existingVariantOptionValuePairs = existingVariantOptionValues.Select(vov => new
                {
                    OptionName = vov.OptionValue?.Option?.Name,
                    OptionValue = vov.OptionValue?.Value
                }).Where(p => p.OptionName != null && p.OptionValue != null).ToHashSet();

                var variantOptionValuesToDelete = existingVariantOptionValues.Where(vov =>
                    vov.OptionValue?.Option?.Name != null && vov.OptionValue?.Value != null &&
                    !newVariantOptionValuePairs.Any(nvov =>
                        nvov.OptionName == vov.OptionValue.Option.Name &&
                        nvov.OptionValue == vov.OptionValue.Value)).ToList();

                if (variantOptionValuesToDelete.Any())
                {
                    await _shopifyRepo.DeleteVariantOptionValueAsync(variantOptionValuesToDelete);
                }

                // Add new variant option values (only those that don't already exist)
                var variantOptionValuesToAdd = newVariantOptionValues.Where(vov =>
                    vov.OptionValue?.Option?.Name != null && vov.OptionValue?.Value != null &&
                    !existingVariantOptionValuePairs.Any(evov =>
                        evov.OptionName == vov.OptionValue.Option.Name &&
                        evov.OptionValue == vov.OptionValue.Value)).ToList();

                if (variantOptionValuesToAdd.Any())
                {
                    await _shopifyRepo.AddVariantOptionValueAsync(variantOptionValuesToAdd);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeVariantOptionValuesAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeInventoryLevelsAsync(JsonElement variantNode, Variant existingVariant)
        {
            try
            {
                if (!variantNode.TryGetProperty("inventoryItem", out var inventoryItemNode) ||
                    !inventoryItemNode.TryGetProperty("inventoryLevels", out var inventoryLevelsNode))
                {
                    return;
                }

                var newInventoryLevels = await AddLocationAndSetInventoryLevelAsync(variantNode, existingVariant, inventoryLevelsNode);
                var existingInventoryLevels = await _shopifyRepo.GetInventoryLevelsByVariantIdAsync(existingVariant.Id);

                var newLocationIds = newInventoryLevels.Select(il => il.Location?.Id).Where(id => id != null).ToHashSet();
                var inventoryLevelsToDelete = existingInventoryLevels
                    .Where(il => il.Location?.Id != null && !newLocationIds.Contains(il.Location.Id))
                    .ToList();
                if (inventoryLevelsToDelete.Any())
                    await _shopifyRepo.DeleteInventoryLevelAsync(inventoryLevelsToDelete);

                var existingLocationIds = existingInventoryLevels.Select(il => il.Location?.Id).Where(id => id != null).ToHashSet();
                var inventoryLevelsToAdd = newInventoryLevels
                    .Where(il => il.Location?.Id != null && !existingLocationIds.Contains(il.Location.Id))
                    .ToList();
                if (inventoryLevelsToAdd.Any())
                    await _shopifyRepo.AddInventoryLevelsAsync(inventoryLevelsToAdd);

                foreach (var newLevel in newInventoryLevels)
                {
                    var tracked = existingInventoryLevels.FirstOrDefault(e =>
                        e.Location?.Id == newLevel.Location?.Id && e.Variant?.Id == newLevel.Variant?.Id);
                    if (tracked != null)
                    {
                        tracked.Available = newLevel.Available;
                        tracked.UpdatedAt = newLevel.UpdatedAt;
                    }
                }
                var updatedLevels = existingInventoryLevels
                    .Where(e => newInventoryLevels.Any(n => n.Location?.Id == e.Location?.Id && n.Variant?.Id == e.Variant?.Id))
                    .ToList();
                if (updatedLevels.Any())
                    await _shopifyRepo.UpdateInventoryLevelsAsync(updatedLevels);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeInventoryLevelsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeVariantPricesAsync(JsonElement variantNode, Variant existingVariant)
        {
            try
            {
                if (!variantNode.TryGetProperty("metafields", out var metafieldsNode))
                {
                    return;
                }

                var newVariantPrices = new List<VariantPrice>();

                foreach (var priceEdge in metafieldsNode.GetProperty("edges").EnumerateArray())
                {
                    var node = priceEdge.GetProperty("node");
                    if (!string.IsNullOrEmpty(node.GetProperty("namespace").GetString()) &&
                        node.GetProperty("namespace").GetString() == variPriceNameSpace)
                    {
                        var value = node.GetProperty("value").GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            var variantPrices = await SetVariantPrices(value);
                            if (variantPrices != null && variantPrices.Any())
                            {
                                foreach (var variantPrice in variantPrices)
                                {
                                    variantPrice.Variant = existingVariant;
                                }
                                 newVariantPrices.AddRange(variantPrices);
                            }
                        }
                    }
                }

                var existingVariantPrices = await _shopifyRepo.GetVariantPricesByVariantIdAsync(existingVariant.Id);

                var newLocationIds = newVariantPrices.Select(vp => vp.Location?.Id).ToHashSet();
                var existingLocationIds = existingVariantPrices.Select(vp => vp.LocationId).ToHashSet();

                var variantPricesToDelete = existingVariantPrices.Where(vp =>
                    !newLocationIds.Contains(vp.LocationId)).ToList();

                if (variantPricesToDelete != null && variantPricesToDelete.Any())
                {
                    await _shopifyRepo.DeleteVariantPriceAsync(variantPricesToDelete);
                }


                if (newVariantPrices != null && newVariantPrices.Any())
                {
                    await _shopifyRepo.UpsertVariantPricesAsync(newVariantPrices);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeVariantPricesAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeOemVariantsAsync(JsonElement variantNode, Variant existingVariant)
        {
            try
            {
                if (!variantNode.TryGetProperty("metafields", out var metafieldsNode))
                {
                    return;
                }

                var newOemVariants = new List<OemVariant>();

                foreach (var priceEdge in metafieldsNode.GetProperty("edges").EnumerateArray())
                {
                    var node = priceEdge.GetProperty("node");
                    if (!string.IsNullOrEmpty(node.GetProperty("namespace").GetString()) &&
                        node.GetProperty("namespace").GetString() == customNameSpace &&
                        node.GetProperty("key").GetString() == oemcomplementairesKey)
                    {
                        var value = node.GetProperty("value").GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            var oemVariants = await SetOEMVariant(value);
                            if (oemVariants != null && oemVariants.Any())
                            {
                                foreach (var oemVariant in oemVariants)
                                {
                                    oemVariant.VariantId = existingVariant.Id;
                                }
                                newOemVariants.AddRange(oemVariants);
                            }
                        }
                    }
                }

                var existingOemVariants = await _shopifyRepo.GetOemVariantsByVariantIdAsync(existingVariant.Id);

                var newOemIds = newOemVariants.Select(ov => ov.OEMId).ToHashSet();
                var existingOemIds = existingOemVariants.Select(ov => ov.OEMId).ToHashSet();

                var oemVariantsToDelete = existingOemVariants.Where(ov =>
                    !newOemIds.Contains(ov.OEMId)).ToList();

                if (oemVariantsToDelete.Any())
                {
                    await _shopifyRepo.DeleteOemVariantAsync(oemVariantsToDelete);
                }

                // Add only new OEM variants (not existing ones)
                var oemVariantsToAdd = newOemVariants.Where(ov =>
                    !existingOemIds.Contains(ov.OEMId)).ToList();

                if (oemVariantsToAdd.Any())
                {
                    await _shopifyRepo.UpdateOemVariantsAsync(oemVariantsToAdd);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeOemVariantsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeTagsAsync(JsonElement node, Product existingProduct)
        {
            try
            {
                var tagSetDto = await SetTagAndProductTagAsync(node, existingProduct);
                if (tagSetDto == null) return;

                var existingProductTags = await _shopifyRepo.GetProductTagsByProductIdAsync(existingProduct.Id) ?? new List<ProductTag>();

                var existingTagIds = existingProductTags.Select(pt => pt.TagId).ToHashSet();
                var existingTags = await _shopifyRepo.GetTagsByIdsAsync(existingTagIds);

                var shopifyTagTitles = tagSetDto.Tags.Select(t => t.Title).ToHashSet();
                var existingTagTitles = existingTags.Select(t => t.Title).ToHashSet();

                var tagsToDelete = existingTags.Where(t => !shopifyTagTitles.Contains(t.Title)).ToList();
                var tagsToUpdate = existingTags.Where(t => shopifyTagTitles.Contains(t.Title)).ToList();
                var tagsToAdd = tagSetDto.Tags.Where(t => !existingTagTitles.Contains(t.Title)).ToList();

                if (tagsToDelete.Any())
                {
                    var productTagsToDelete = existingProductTags.Where(pt =>
                        tagsToDelete.Any(t => t.Id == pt.TagId)).ToList();
                    await _shopifyRepo.DeleteProductTagAsync(productTagsToDelete);
                }

                foreach (var tagToUpdate in tagsToUpdate)
                {
                    var shopifyTag = tagSetDto.Tags.FirstOrDefault(t => t.Title == tagToUpdate.Title);
                    if (shopifyTag != null)
                    {
                        if (tagToUpdate.Title != shopifyTag.Title ||
                            tagToUpdate.Title_en != shopifyTag.Title_en)
                        {
                            tagToUpdate.Title = shopifyTag.Title;
                            tagToUpdate.Title_en = shopifyTag.Title_en;
                            tagToUpdate.CreatedAt = shopifyTag.CreatedAt;
                            tagToUpdate.UpdatedAt = shopifyTag.UpdatedAt;
                            await _shopifyRepo.UpdateTagAsync(tagToUpdate);
                        }
                    }
                }

                if (tagsToAdd.Any())
                {
                    await _shopifyRepo.AddTagsAsync(tagsToAdd);
                }

                var shopifyProductTagPairs = tagSetDto.ProductTags.Select(pt => pt.Tag?.Title).ToHashSet();
                var existingProductTagPairs = existingProductTags.Select(pt => pt.Tag?.Title).ToHashSet();

                var productTagsToDeleteFromShopify = existingProductTags.Where(pt =>
                    pt.Tag?.Title != null && !shopifyProductTagPairs.Contains(pt.Tag.Title)).ToList();

                var productTagsToUpdate = existingProductTags.Where(pt =>
                    pt.Tag?.Title != null && shopifyProductTagPairs.Contains(pt.Tag.Title)).ToList();

                var productTagsToAdd = tagSetDto.ProductTags.Where(pt =>
                    pt.Tag?.Title != null && !existingProductTagPairs.Contains(pt.Tag.Title)).ToList();

              

                if (productTagsToAdd.Any())
                {
                    await _shopifyRepo.AddProductTagsAsync(productTagsToAdd);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeTagsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task SynchronizeCollectionsAsync(JsonElement collectionsNode, Product existingProduct)
        {
            try
            {
                var existingProductCollections = await _shopifyRepo.GetProductCollectionsByProductIdAsync(existingProduct.Id) ?? new List<ProductCollection>();

                var existingCollectionIds = existingProductCollections.Select(pc => pc.CollectionId).ToHashSet();
                var existingCollections = await _shopifyRepo.GetCollectionsByIdsAsync(existingCollectionIds);

                var shopifyCollectionIds = new HashSet<string>();
                if (collectionsNode.TryGetProperty("edges", out var edgesNode))
                {
                    foreach (var edge in edgesNode.EnumerateArray())
                    {
                        if (edge.TryGetProperty("node", out var node) &&
                            node.TryGetProperty("id", out var idNode))
                        {
                            var shopifyId = idNode.GetString();
                            if (!string.IsNullOrEmpty(shopifyId))
                            {
                                shopifyCollectionIds.Add(shopifyId);
                            }
                        }
                    }
                }

                var existingCollectionShopifyIds = existingCollections.Select(c => c.ShopifyId).Where(id => id != null).ToHashSet();

                var collectionsToDelete = existingCollections.Where(c =>
                    c.ShopifyId != null && !shopifyCollectionIds.Contains(c.ShopifyId)).ToList();
                var collectionsToUpdate = existingCollections.Where(c =>
                    c.ShopifyId != null && shopifyCollectionIds.Contains(c.ShopifyId)).ToList();

                if (collectionsToDelete.Any())
                {
                    var productCollectionsToDelete = existingProductCollections.Where(pc =>
                        collectionsToDelete.Any(c => c.Id == pc.CollectionId)).ToList();
                    await _shopifyRepo.DeleteProductCollectionAsync(productCollectionsToDelete);
                }

                foreach (var collectionToUpdate in collectionsToUpdate)
                {
                    if (collectionsNode.TryGetProperty("edges", out var edgesForUpdate))
                    {
                        foreach (var edge in edgesForUpdate.EnumerateArray())
                        {
                            if (edge.TryGetProperty("node", out var node) &&
                                node.TryGetProperty("id", out var idNode) &&
                                idNode.GetString() == collectionToUpdate.ShopifyId)
                            {
                                var shopifyTitle = node.GetProperty("title").GetString();
                                var shopifyDescription = node.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                                var shopifyImage = node.TryGetProperty("image", out var img) &&
                                               img.ValueKind == JsonValueKind.Object &&
                                               img.TryGetProperty("src", out var src)
                                               ? src.GetString() : null;

                                if (collectionToUpdate.Title != shopifyTitle ||
                                    collectionToUpdate.Description != shopifyDescription ||
                                    collectionToUpdate.Image != shopifyImage)
                                {
                                    collectionToUpdate.Title = shopifyTitle;
                                    collectionToUpdate.Description = shopifyDescription;
                                    collectionToUpdate.Image = shopifyImage;
                                    await _shopifyRepo.UpdateCollectionAsync(collectionToUpdate);
                                }
                                break;
                            }
                        }
                    }
                }

                

                var productCollectionsToAdd = new List<ProductCollection>();
                foreach (var shopifyCollectionId in shopifyCollectionIds)
                {
                    var existingCollection = existingCollections.FirstOrDefault(c => c.ShopifyId == shopifyCollectionId);

                    if (existingCollection != null)
                    {
                        var existingProductCollection = existingProductCollections.FirstOrDefault(pc =>
                            pc.CollectionId == existingCollection.Id);

                        if (existingProductCollection == null)
                        {
                            productCollectionsToAdd.Add(new ProductCollection
                            {
                                ProductId = existingProduct.Id,
                                CollectionId = existingCollection.Id
                            });
                        }
                    }
                    else
                    {
                        var collection = await _shopifyRepo.GetCollectionByShopifyId(shopifyCollectionId);
                        if (collection == null)
                        {
                            if (collectionsNode.TryGetProperty("edges", out var edgesForCollection))
                            {
                                foreach (var edge in edgesForCollection.EnumerateArray())
                                {
                                    if (edge.TryGetProperty("node", out var node) &&
                                        node.TryGetProperty("id", out var idNode) &&
                                        idNode.GetString() == shopifyCollectionId)
                                    {
                                        collection = new Collection
                                        {
                                            ShopifyId = shopifyCollectionId,
                                            Title = node.GetProperty("title").GetString(),
                                            Description = node.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                                            Image = node.TryGetProperty("image", out var img) &&
                                                   img.ValueKind == JsonValueKind.Object &&
                                                   img.TryGetProperty("src", out var src)
                                                   ? src.GetString() : null
                                        };
                                        await _shopifyRepo.AddCollectionsAsync(new List<Collection> { collection });
                                        break;
                                    }
                                }
                            }
                        }

                        if (collection != null)
                        {
                            productCollectionsToAdd.Add(new ProductCollection
                            {
                                ProductId = existingProduct.Id,
                                CollectionId = collection.Id
                            });
                        }
                    }
                }

                if (productCollectionsToAdd.Any())
                {
                    await _shopifyRepo.AddProductCollectionsAsync(productCollectionsToAdd);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SynchronizeCollectionsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Imports product images from Shopify for all products updated after the specified date
        /// </summary>
        /// <param name="updatedAfter">Optional filter for products updated after this date</param>
        /// <returns>Response with success status and count of imported images</returns>
        public async Task<Response> ImportProductImagesAsync(DateTime? updatedAfter = null)
        {
            _logger.LogInformation("[ShopifyService] ImportProductImagesAsync called with filter: {Filter}", updatedAfter?.ToString("yyyy-MM-dd HH:mm:ss") ?? "none");
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                _logger.LogDebug("[ShopifyService] HTTP client configured for product images import");

                string afterCursor = null;
                bool hasNextPage = true;
                int totalImported = 0;
                string filter = updatedAfter.HasValue ? $"created_at:>{updatedAfter} OR updated_at:>{updatedAfter.Value}" : "";

                int pageCount = 0;
                while (hasNextPage)
                {
                    pageCount++;
                    _logger.LogDebug("[ShopifyService] Processing product images page {Page}", pageCount);

                    var query = $@"
                        query ($first: Int!, $after: String, $query: String) {{
                            products(first: $first, after: $after, query: $query) {{
                                pageInfo {{
                                    hasNextPage
                                    endCursor
                                }}
                                edges {{
                                    node {{
                                        id
                                        images(first: 50) {{
                                            edges {{
                                                node {{
                                                    id
                                                    src
                                                }}
                                            }}
                                        }}
                                    }}
                                }}
                            }}
                        }}";

                    var variables = new
                    {
                        first = _productPageSize,
                        after = afterCursor,
                        query = filter
                    };

                    var requestBody = new { query, variables };
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("", content);
                    var json = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement.GetProperty("data").GetProperty("products");

                    hasNextPage = root.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
                    afterCursor = root.GetProperty("pageInfo").GetProperty("endCursor").GetString();

                    var productImages = new List<ProductImage>();

                    foreach (var edge in root.GetProperty("edges").EnumerateArray())
                    {
                        var productNode = edge.GetProperty("node");
                        var productShopifyId = productNode.GetProperty("id").GetString();

                        if (productNode.TryGetProperty("images", out var imagesNode))
                        {
                            foreach (var imageEdge in imagesNode.GetProperty("edges").EnumerateArray())
                            {
                                var imageNode = imageEdge.GetProperty("node");
                                var product = await _shopifyRepo.GetProductByShopifyIdAsync(productShopifyId);

                                if (product == null)
                                {
                                    continue;
                                }
                                else
                                {
                                    var productImage = new ProductImage
                                    {
                                        ProductId = product.Id,
                                        ImageSrc = imageNode.GetProperty("src").GetString(),
                                        ImageShopifyId = imageNode.GetProperty("id").GetString(),
                                        LocalCreatedAt = DateTime.UtcNow,

                                    };
                                    productImages.Add(productImage);
                                }
                            }
                        }
                    }

                    if (productImages.Any())
                    {
                        await _shopifyRepo.AddProductImagesAsync(productImages);
                        totalImported += productImages.Count;
                    }
                }

                _logger.LogInformation("[ShopifyService] ImportProductImagesAsync completed successfully, imported {Count} images across {PageCount} pages", totalImported, pageCount);
                return ResponseHelper.Success($"Successfully imported {totalImported} product images", new { TotalImported = totalImported });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error occurred in ImportProductImagesAsync: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductImagesAsync", 1, ex.Message, ex.ToString());
                return ResponseHelper.Conflict($"Failed to import product images: {ex.Message}");
            }
        }


        /// <summary>
        /// Fetches a Shopify order by its numeric ID using GraphQL API.
        /// Includes order details, fulfillment orders, and line items.
        /// </summary>
        /// <param name="shopifyOrderId">The numeric Shopify order ID</param>
        /// <returns>JsonElement containing the GraphQL response data, or null if not found</returns>
        public async Task<JsonElement?> FetchShopifyOrderByIdAsync(long shopifyOrderId)
        {
            _logger.LogInformation("[ShopifyService] Fetching Shopify order with ID: {ShopifyOrderId}", shopifyOrderId);
            
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);
                
                // GraphQL query matching the Python script
                var query = @"
                query ($id: ID!, $q: String!) {
                  order(id: $id) {
                    id
                    name
                    createdAt
                    sourceName
                    currencyCode
                    totalPriceSet {
                      shopMoney {
                        amount
                        currencyCode
                      }
                    }
                    retailLocation { id name }
                    customer { firstName lastName email phone }
                    lineItems(first: 250) {
                      nodes { id sku quantity name variant { id sku } }
                    }
                    shippingAddress { name phone address1 address2 city province country zip }
                    billingAddress  { name phone address1 address2 city province country zip }
                  }
                  fulfillmentOrders(first: 50, query: $q) {
                    nodes {
                      id
                      status
                      assignedLocation { location { id name } }
                      destination {
                        ... on FulfillmentOrderDestination {
                          firstName
                          lastName
                          email
                          phone
                          address1
                          address2
                          city
                          province
                          countryCode
                          zip
                        }
                      }
                      lineItems(first: 250) {
                        nodes {
                          id
                          remainingQuantity
                          lineItem { id sku quantity name variant { id sku } }
                        }
                      }
                    }
                  }
                }";

                var orderGid = $"gid://shopify/Order/{shopifyOrderId}";
                var variables = new
                {
                    id = orderGid,
                    q = $"order_id:{shopifyOrderId}"
                };

                var requestBody = new { query, variables };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                _logger.LogDebug("[ShopifyService] Sending GraphQL request for order {ShopifyOrderId}", shopifyOrderId);
                
                var response = await client.PostAsync("", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[ShopifyService] GraphQL request failed with status: {StatusCode}, response: {Response}", response.StatusCode, json);
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errors))
                {
                    _logger.LogError("[ShopifyService] GraphQL errors: {Errors}", errors.ToString());
                    return null;
                }

                if (!root.TryGetProperty("data", out var data) || 
                    !data.TryGetProperty("order", out var order) || 
                    order.ValueKind == JsonValueKind.Null)
                {
                    _logger.LogWarning("[ShopifyService] Order {ShopifyOrderId} not found in Shopify", shopifyOrderId);
                    return null;
                }

                _logger.LogInformation("[ShopifyService] Successfully fetched order {ShopifyOrderId} from Shopify", shopifyOrderId);
                
                // Return a clone of the data element that won't be disposed
                return JsonSerializer.Deserialize<JsonElement>(data.GetRawText());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Error fetching order {ShopifyOrderId}: {Message}", shopifyOrderId, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "FetchShopifyOrderByIdAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private PaginationInfo PaginatedHistoryStatus(HistoryInventoryFilterDto filterDto, (int TotalCount, List<HistoryInventoryStatusDto> Data) data)
        {
            var totalItemCount = data.TotalCount;
            int totalPages = filterDto.page_size > 0 ? (int)Math.Ceiling((double)totalItemCount / filterDto.page_size) : 0;

            var pagination = new PaginationInfo
            {
                TotalItemCount = totalItemCount,
                PageNo = filterDto.page_no,
                PerPage = filterDto.page_size,
                TotalPages = totalPages,
                NextPage = filterDto.page_no < totalPages ? filterDto.page_no + 1 : 0,
                PrevPage = filterDto.page_no > 1 ? filterDto.page_no - 1 : 0
            };

            _logger.LogInformation("[ShopifyService] Successfully fetched {TotalItemCount} history records for page {PageNo} with {TotalPages} pages", totalItemCount, filterDto.page_no, totalPages);
            return pagination;
        }

        /// <summary>
        /// Sends webhook data to RabbitMQ queue via WebHookRMQService.
        /// This method acts as a wrapper to route webhook messages through the business logic layer.
        /// </summary>
        /// <param name="jsonBody">The JSON body of the webhook message.</param>
        /// <param name="queueName">The name of the queue to send the message to.</param>
        public async Task SendWebhookToQueueAsync(string jsonBody, string queueName)
        {
            _logger.LogDebug("[ShopifyService] Routing webhook to queue: {QueueName}", queueName);
            try
            {
                await _webHookRMQService.SendDataToQueue(jsonBody, queueName);
                _logger.LogInformation("[ShopifyService] Successfully routed webhook to queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Failed to route webhook to queue {QueueName}: {Message}", queueName, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SendWebhookToQueueAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Sends update data to RabbitMQ queue via ShopifyUpdateRMQService.
        /// This method acts as a wrapper to route update messages through the business logic layer.
        /// </summary>
        /// <param name="jsonBody">The JSON body of the update message.</param>
        /// <param name="queueName">The name of the queue to send the message to.</param>
        public async Task SendUpdateToQueueAsync(string jsonBody, string queueName)
        {
            _logger.LogDebug("[ShopifyService] Routing update to queue: {QueueName}", queueName);
            try
            {
                await _shopifyUpdateRMQService.SendDataToQueue(jsonBody, queueName);
                _logger.LogInformation("[ShopifyService] Successfully routed update to queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyService] Failed to route update to queue {QueueName}: {Message}", queueName, ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "SendUpdateToQueueAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

    }

    public class PageCursorTracker
    {
        public Dictionary<int, string> PageCursors { get; set; } = new(); // Page number → endCursor
        public int PageSize { get; set; }
    }
}
