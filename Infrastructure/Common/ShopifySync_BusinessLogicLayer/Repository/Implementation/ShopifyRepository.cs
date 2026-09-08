using Microsoft.EntityFrameworkCore;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO;
using ShopifySync_DataAccessLayer.Enum;
using ShopifySync_DataAccessLayer.Model;
using System.Text.Json;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class ShopifyRepository : IShopifyRepository
    {
        private readonly ShopifySyncDbContext _context;
        /// <summary>
        /// Initializes a new instance of the <see cref="ShopifyRepository"/> class.
        /// </summary>
        /// <param name="context">The database context for data access.</param>
        public ShopifyRepository(ShopifySyncDbContext context)
        {
            _context = context;
        }

        public async Task<List<Variant>> GetVariantsByProductIdAsync(int productId)
        {

            return await _context.Variants.Where(v => v.ProductId == productId).ToListAsync();

        }

        public async Task<Variant?> GetVariantByIdAsync(int variantId)
        {
            return await _context.Variants
                .Include(v => v.OEM)
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);
        }

        public async Task<Variant?> GetVariantByShopifyIdAsync(string variantShopifyId)
        {
            return await _context.Variants
                .Include(v => v.OEM)
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.ShopifyId == variantShopifyId);
        }

        public async Task UpdateVariantAsync(Variant variant)
        {

            _context.Variants.Update(variant);
            await _context.SaveChangesAsync();


        }

        public async Task UpdateProductAsync(Product product)
        {

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

        }

        public async Task<Product?> GetProductByShopifyIdAsync(string shopifyId)
        {

            return await _context.Products.FirstOrDefaultAsync(p => p.ShopifyId == shopifyId);

        }

        // Optimized: AddProductsAsync
        public async Task AddProductsAsync(List<Product> products)
        {

            var batchShopifyIds = products.Select(p => p.ShopifyId).ToList();
            var existingShopifyIds = await _context.Products
                .Where(p => batchShopifyIds.Contains(p.ShopifyId))
                .Select(p => p.ShopifyId)
                .ToListAsync();
            var newProducts = products
                .Where(p => !existingShopifyIds.Contains(p.ShopifyId))
                .ToList();
            if (newProducts.Any())
            {
                await _context.Products.AddRangeAsync(newProducts);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Adds a list of inventory levels to the database if they do not already exist.
        /// </summary>
        /// <param name="inventoryLevels">The list of inventory levels to add.</param>
        public async Task AddInventoryLevelsAsync(List<InventoryLevel> inventoryLevels)
        {

            var batchKeys = inventoryLevels.Select(x => x.VariantId).ToList();
            var existingInventory = await _context.InventoryLevels
                .Where(x => batchKeys.Contains(x.VariantId)).ToListAsync();

            var existing = existingInventory
                .Select(x => new { x.VariantId, x.LocationId })
                .ToList();
            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.VariantId, e.LocationId)));
            var newInventoryLevels = inventoryLevels
                .Where(x => !existingSet.Contains((x.VariantId, x.LocationId)))
                .ToList();
            if (newInventoryLevels.Any())
            {
                await _context.InventoryLevels.AddRangeAsync(newInventoryLevels);

            }
            if (existingInventory != null && existingInventory.Any())
            {
                _context.InventoryLevels.UpdateRange(existingInventory);
            }
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Update  list of inventory levels to the database if they are already exist.
        /// </summary>
        /// <param name="inventoryLevels">The list of inventory levels to add.</param>
        public async Task UpdateInventoryLevelsAsync(List<InventoryLevel> inventoryLevels)
        {
            _context.InventoryLevels.UpdateRange(inventoryLevels);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a list of variant option values to the database if they do not already exist.
        /// </summary>
        /// <param name="variantOptionValues">The list of variant option values to add.</param>
        public async Task AddVariantOptionValueAsync(List<VariantOptionValue> variantOptionValues)
        {

            var batchKeys = variantOptionValues.Select(x => x.VariantId).ToList();
            var existing = await _context.VariantOptionValues
                .Where(x => batchKeys.Contains(x.VariantId))
                .Select(x => new { x.VariantId, x.OptionValueId })
                .ToListAsync();
            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.VariantId, e.OptionValueId)));
            var newValues = variantOptionValues
                .Where(x => !existingSet.Contains((x.VariantId, x.OptionValueId)))
                .ToList();
            if (newValues.Any())
            {
                await _context.VariantOptionValues.AddRangeAsync(newValues);
                await _context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// Adds a list of options to the database if they do not already exist.
        /// </summary>
        /// <param name="options">The list of options to add.</param>
        public async Task AddOptionsAsync(List<Option> options)
        {

            var names = options.Select(o => o.Name).ToList();
            var existingNames = await _context.Options
                .Where(o => names.Contains(o.Name))
                .Select(o => o.Name)
                .ToListAsync();
            var newOptions = options
                .Where(opt => !existingNames.Contains(opt.Name))
                .ToList();
            if (newOptions.Any())
            {
                await _context.Options.AddRangeAsync(newOptions);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Adds a list of option values to the database if they do not already exist.
        /// </summary>
        /// <param name="optionValues">The list of option values to add.</param>
        public async Task AddOptionValuesAsync(List<OptionValue> optionValues)
        {

            var batchKeys = optionValues.Select(ov => ov.Value).ToList();

            var existing = await _context.OptionValues
                .Where(ov => batchKeys.Contains(ov.Value))
                .Select(ov => new { ov.OptionId, ov.Value })
                .ToListAsync();

            var existingSet = new HashSet<(int, string)>(existing.Select(e => (e.OptionId, e.Value)));
            var newValues = optionValues
                .Where(ov => !existingSet.Contains((ov.OptionId, ov.Value)))
                .ToList();

            if (newValues.Any())
            {
                await _context.OptionValues.AddRangeAsync(newValues);
                await _context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// Adds a list of product options to the database if they do not already exist.
        /// </summary>
        /// <param name="productOptions">The list of product options to add.</param>
        public async Task AddProductOptionAsync(List<ProductOption> productOptions)
        {

            var batchKeys = productOptions.Select(po => po.ProductId).ToList();

            var existing = await _context.ProductOptions
                .Where(po => batchKeys.Contains(po.ProductId))
                .Select(po => new { po.ProductId, po.OptionId })
                .ToListAsync();

            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.ProductId, e.OptionId)));
            var newOptions = productOptions
                .Where(po => !existingSet.Contains((po.ProductId, po.OptionId)))
                .ToList();
            if (newOptions.Any())
            {
                await _context.ProductOptions.AddRangeAsync(newOptions);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Adds a list of collections to the database if they do not already exist.
        /// </summary>
        /// <param name="collections">The list of collections to add.</param>
        public async Task AddCollectionsAsync(List<Collection> collections)
        {

            var batchShopifyIds = collections.Select(c => c.ShopifyId).ToList();
            var existingShopifyIds = await _context.Collections
                .Where(c => batchShopifyIds.Contains(c.ShopifyId))
                .Select(c => c.ShopifyId)
                .ToListAsync();
            var newCollections = collections
                .Where(c => !existingShopifyIds.Contains(c.ShopifyId))
                .ToList();
            if (newCollections.Any())
            {
                await _context.Collections.AddRangeAsync(newCollections);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Adds a list of product collections to the database if they do not already exist.
        /// </summary>
        /// <param name="productCollections">The list of product collections to add.</param>
        public async Task AddProductCollectionsAsync(List<ProductCollection> productCollections)
        {

            var batchKeys = productCollections.Select(pc => pc.ProductId).ToList();

            var existing = await _context.ProductCollections
                .Where(pc => batchKeys.Contains(pc.ProductId))
                .Select(pc => new { pc.ProductId, pc.CollectionId })
                .ToListAsync();

            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.ProductId, e.CollectionId)));
            var newItems = productCollections
                .Where(pc => !existingSet.Contains((pc.ProductId, pc.CollectionId)))
                .ToList();

            if (newItems != null && newItems.Any())
            {
                await _context.ProductCollections.AddRangeAsync(newItems);
                await _context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// Adds a list of tags to the database if they do not already exist.
        /// </summary>
        /// <param name="tags">The list of tags to add.</param>
        public async Task AddTagsAsync(List<Tag> tags)
        {

            var batchTitles = tags.Select(t => t.Title).ToList();
            var existingTitles = await _context.Tags
                .Where(t => batchTitles.Contains(t.Title))
                .Select(t => t.Title)
                .ToListAsync();
            var newTags = tags
                .Where(tag => !existingTitles.Contains(tag.Title))
                .ToList();
            if (newTags.Any())
            {
                await _context.Tags.AddRangeAsync(newTags);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Adds a list of product tags to the database if they do not already exist.
        /// </summary>
        /// <param name="productTags">The list of product tags to add.</param>
        public async Task AddProductTagsAsync(List<ProductTag> productTags)
        {

            var batchKeys = productTags.Select(pt => pt.ProductId).ToList();

            var existing = await _context.ProductTags
                .Where(pt => batchKeys.Contains(pt.ProductId))
                .Select(pt => new { pt.ProductId, pt.TagId })
                .ToListAsync();

            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.ProductId, e.TagId)));
            var newProductTags = productTags
                .Where(pt => !existingSet.Contains((pt.ProductId, pt.TagId)))
                .ToList();
            if (newProductTags.Any())
            {
                await _context.ProductTags.AddRangeAsync(newProductTags);
                await _context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// Adds a list of variants to the database if they do not already exist.
        /// </summary>
        /// <param name="variants">The list of variants to add.</param>
        public async Task AddVariants(List<Variant> variants)
        {

            var batchShopifyIds = variants.Select(v => v.ShopifyId).ToList();
            var existingShopifyIds = await _context.Variants
                .Where(v => batchShopifyIds.Contains(v.ShopifyId))
                .Select(v => v.ShopifyId)
                .ToListAsync();
            var newVariants = variants
                .Where(v => !existingShopifyIds.Contains(v.ShopifyId))
                .ToList();
            if (newVariants.Any())
            {
                await _context.Variants.AddRangeAsync(newVariants);
                await _context.SaveChangesAsync();
            }

        }
        /// <summary>
        /// Adds a list of locations to the database if they do not already exist.
        /// </summary>
        /// <param name="locations">The list of locations to add.</param>
        public async Task AddLocationsAsync(List<Location> locations)
        {

            var batchShopifyIds = locations.Select(l => l.ShopifyId).ToList();
            var existingShopifyIds = await _context.Locations
                .Where(l => batchShopifyIds.Contains(l.ShopifyId))
                .Select(l => l.ShopifyId)
                .ToListAsync();
            var newLocations = locations
                .Where(l => !existingShopifyIds.Contains(l.ShopifyId))
                .ToList();
            if (newLocations.Any())
            {
                await _context.Locations.AddRangeAsync(newLocations);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Adds a list of vendors to the database if they do not already exist.
        /// </summary>
        /// <param name="vendors">The list of vendors to add.</param>
        public async Task AddVendorsAsync(List<Vendor> vendors)
        {

            var existingShopifyIds = _context.Vendors
                .Select(v => v.Title)
                .ToHashSet();

            var newVendors = vendors
                .Where(v => !existingShopifyIds.Contains(v.Title))
                .ToList();

            if (newVendors != null && newVendors.Any())
            {
                _context.Vendors.AddRange(newVendors);
                await _context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// Adds a new history inventory record to the database.
        /// </summary>
        /// <param name="historyInventory">The history inventory record to add.</param>
        /// <returns>The ID of the newly added history inventory record.</returns>
        public async Task<int> AddHistoryInventory(HistoryInventory historyInventory)
        {

            _context.HistoryInventory.Add(historyInventory);
            await _context.SaveChangesAsync();
            var history = await _context.HistoryInventory.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            return history.Id;

        }

        /// <summary>
        /// Updates an existing history inventory record in the database.
        /// </summary>
        /// <param name="historyId">The ID of the history inventory record to update.</param>
        /// <param name="historyInventory">The updated history inventory data.</param>
        public async Task UpdateHistoryInventory(int historyId, HistoryInventory historyInventory)
        {

            var history = await _context.HistoryInventory.FirstOrDefaultAsync(x => x.Id == historyId);

            if (history != null)
            {
                history.InProgress = historyInventory.InProgress;
                history.TotalProductsProcessed = historyInventory.TotalProductsProcessed;
                history.ErrorMessage = historyInventory.ErrorMessage;
                history.TotalCursorsProcessed = historyInventory.TotalCursorsProcessed;
                history.TotalCursorsFailed = historyInventory.TotalCursorsFailed;
                history.LastRecordDate = historyInventory.LastRecordDate;
                history.IsSuccess = historyInventory.IsSuccess;
                history.LastSyncDate = historyInventory.LastSyncDate;
                history.ProcessEndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Updates the status of an existing history inventory record in the database.
        /// </summary>
        /// <param name="historyId">The ID of the history inventory record to update.</param>
        /// <param name="historyInventory">The updated history inventory data.</param>
        public async Task StatusUpdateHistoryInventory(int historyId, HistoryInventory historyInventory)
        {

            var history = await _context.HistoryInventory.FirstOrDefaultAsync(x => x.Id == historyId);
            if (history != null)
            {
                history.InProgress = historyInventory.InProgress;
                history.IsSuccess = historyInventory.IsSuccess;
                await _context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// Gets the latest history inventory record from the database.
        /// </summary>
        /// <returns>The latest HistoryInventory object.</returns>
        public async Task<HistoryInventory> GetHistoryStatus()
        {

            var history = await _context.HistoryInventory.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            return history;

        }

        /// <summary>
        /// Gets paginated history status records with filtering support.
        /// </summary>
        /// <param name="filterDto">Filter criteria including pagination parameters.</param>
        /// <returns>A tuple containing total count and paginated history data.</returns>
        public async Task<(int TotalCount, List<HistoryInventoryStatusDto> Data)> GetHistoryStatusPaginated(HistoryInventoryFilterDto filterDto)
        {
            var query = _context.HistoryInventory.AsQueryable();

            // Apply filters
            query = ApplyFilters(filterDto, query);

            // Order by latest first
            query = query.OrderByDescending(h => h.Id);

            var totalCount = await query.CountAsync();

            var historyData = await query
                .Skip((filterDto.page_no - 1) * filterDto.page_size)
                .Take(filterDto.page_size)
                .Select(h => new HistoryInventoryStatusDto
                {
                    Id = h.Id,
                    TotalProductsProcessed = h.TotalProductsProcessed,
                    TotalCursors = h.TotalCursors,
                    TotalCursorsProcessed = h.TotalCursorsProcessed,
                    TotalCursorsFailed = h.TotalCursorsFailed,
                    InProgress = h.InProgress,
                    IsSuccess = h.IsSuccess,
                    LastSyncDate = h.LastSyncDate,
                    LastRecordDate = h.LastRecordDate,
                    ProcessStartDate = h.ProcessStartDate,
                    ProcessEndDate = h.ProcessEndDate,
                    ErrorMessage = h.ErrorMessage,
                    StatusText = h.InProgress ? "In Progress" : h.IsSuccess ? "Success" : "Failed",
                    ProcessingProgress = h.TotalCursors > 0
                        ? (int)((double)h.TotalCursorsProcessed / h.TotalCursors * 100)
                        : 0
                })
                .ToListAsync();

            return (totalCount, historyData);
        }

        private static IQueryable<HistoryInventory> ApplyFilters(HistoryInventoryFilterDto filterDto, IQueryable<HistoryInventory> query)
        {
            if (filterDto.fromDate.HasValue)
            {
                query = query.Where(h => h.ProcessStartDate >= filterDto.fromDate.Value);
            }

            if (filterDto.toDate.HasValue)
            {
                query = query.Where(h => h.ProcessStartDate <= filterDto.toDate.Value);
            }

            if (filterDto.inProgress.HasValue)
            {
                query = query.Where(h => h.InProgress == filterDto.inProgress.Value);
            }

            if (filterDto.isSuccess.HasValue)
            {
                query = query.Where(h => h.IsSuccess == filterDto.isSuccess.Value);
            }

            if (!string.IsNullOrEmpty(filterDto.search))
            {
                string searchTerm = filterDto.search.ToLower();
                query = query.Where(h => h.ErrorMessage != null && h.ErrorMessage.ToLower().Contains(searchTerm));
            }

            return query;
        }

        public async Task<List<Location>> GetLocationsByShopifyIdsAsync(List<string> shopifyIds)
        {

            var locations = await _context.Locations.Where(x => shopifyIds.Contains(x.ShopifyId)).ToListAsync();
            return locations;

        }

        public async Task<Location> GetLocationsByShopifyIdAsync(string shopifyIds)
        {

            var location = await _context.Locations.Where(x => shopifyIds.Contains(x.ShopifyId)).FirstOrDefaultAsync();
            return location;

        }

        /// <summary>
        /// Gets an existing vendor by name or creates a new one if it does not exist.
        /// </summary>
        /// <param name="vendorName">The name of the vendor.</param>
        /// <returns>The Vendor object.</returns>
        public async Task<Vendor> GetOrCreateVendorByNameAsync(string vendorName)
        {

            var vendor = await _context.Vendors.FirstOrDefaultAsync(x => x.Title == vendorName);
            if (vendor == null)
            {
                vendor = new Vendor { Title = vendorName };
                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();
            }
            return vendor;

        }

        /// <summary>
        /// Gets an existing product type by name or creates a new one if it does not exist.
        /// </summary>
        /// <param name="productTypeName">The name of the product type.</param>
        /// <returns>The ProductType object.</returns>
        public async Task<ProductType> GetOrCreateProductTypeByNameAsync(string productTypeName)
        {
            if (string.IsNullOrWhiteSpace(productTypeName))
            {
                return null;
            }

            var productType = await _context.ProductTypes.FirstOrDefaultAsync(x => x.Name == productTypeName);
            if (productType == null)
            {
                productType = new ProductType 
                { 
                    Name = productTypeName, 
                    LocalCreatedAt = DateTime.UtcNow,
                    LocalUpdatedAt = DateTime.UtcNow
                };
                _context.ProductTypes.Add(productType);
                await _context.SaveChangesAsync();
            }
            return productType;
        }

        /// <summary>
        /// Add record to ShopifyDataQueue in case of failur
        /// </summary>
        /// <param name="shopifyDataQueue">The name of the shopifyDataQueue.</param>
        public async Task AddShopifyQueueAsync(ShopifyDataQueue shopifyDataQueue)
        {
            try
            {
                _context.ShopifyDataQueues.Add(shopifyDataQueue);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Get product from queue
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<ShopifyDataQueue> GetProductFromQueueAsync(string productId)
        {
           return await _context.ShopifyDataQueues.FirstOrDefaultAsync( x=>x.ProductShopifyId==productId);
        }

        /// <summary>
        /// Get failed queue
        /// </summary>
        /// <returns></returns>
        public async Task<List<ShopifyDataQueue>> GetFailedQueueAsync()
        {
            try
            {
                return await _context.ShopifyDataQueues
              .Where(q => q.Status == QueueStatus.Failed && q.RetryCount < 10)
              .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Update queue
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isSuccessfull"></param>
        /// <returns></returns>
        public async Task UpdateShopifyQueueAsync(ShopifyDataQueue item, bool isSuccessfull)
        {
            try
            {
                if (isSuccessfull)
                {
                    item.Status = QueueStatus.Completed;
                    item.LastError = null;
                    item.LastAttemptAt = DateTime.UtcNow;
                    item.RetryCount++;
                }
                else
                {
                    item.RetryCount++;
                    item.LastError = item.LastError;
                    item.LastAttemptAt = DateTime.UtcNow;
                }

            }
            catch (Exception ex)
            {

            }

            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Gets a list of collections by their Shopify IDs.
        /// </summary>
        /// <param name="shopifyId"></param>
        /// <returns></returns>
        public async Task<List<Product>> GetProductsByShopifyIds(string shopifyId)
        {

            return await _context.Products
                .Include(x => x.ProductCollections).ThenInclude(x => x.Collection)
                .Where(x => x.ShopifyId == shopifyId)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a collection by its Shopify ID.
        /// </summary>
        /// <param name="shopifyId"></param>
        /// <returns></returns>
        public async Task<Collection> GetCollectionByShopifyId(string shopifyId)
        {
            return await _context.Collections
                .Where(x => x.ShopifyId == shopifyId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Adds a list of variant prices to the database if they do not already exist.
        /// </summary>
        /// <param name="variantPrices">The list of variant prices to add.</param>
        public async Task AddVariantPricesAsync(List<VariantPrice> variantPrices)
        {

            var batchKeys = variantPrices.Select(x => new { x.VariantId, x.LocationId }).ToList();
            var existing = await _context.VariantPrices
                .Where(x => batchKeys.Any(k => k.VariantId == x.VariantId && k.LocationId == x.LocationId))
                .Select(x => new { x.VariantId, x.LocationId })
                .ToListAsync();
            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.VariantId, e.LocationId)));
            var newVariantPrices = variantPrices
                .Where(x => !existingSet.Contains((x.VariantId, x.LocationId)))
                .ToList();
            if (newVariantPrices.Any())
            {
                await _context.VariantPrices.AddRangeAsync(newVariantPrices);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// add variant prices
        /// </summary>
        /// <param name="variantPrices"></param>
        /// <returns></returns>
        public async Task AddVariantPriceAsync(List<VariantPrice> variantPrices)
        {
            if (variantPrices!= null && variantPrices.Any())
            {
                await _context.VariantPrices.AddRangeAsync(variantPrices);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Adds a list of OEM variants to the database if they do not already exist.
        /// </summary>
        /// <param name="oemVariants">The list of OEM variants to add.</param>
        public async Task AddOemVariantsAsync(List<OemVariant> oemVariants)
        {

            var batchKeys = oemVariants.Select(x => new { x.VariantId, x.OEMId }).ToList();
            var existing = await _context.OemVariants
                .Where(x => batchKeys.Any(k => k.VariantId == x.VariantId && k.OEMId == x.OEMId))
                .Select(x => new { x.VariantId, x.OEMId })
                .ToListAsync();
            var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.VariantId, e.OEMId)));
            var newOemVariants = oemVariants
                .Where(x => !existingSet.Contains((x.VariantId, x.OEMId)))
                .ToList();
            if (newOemVariants.Any())
            {
                await _context.OemVariants.AddRangeAsync(newOemVariants);
                await _context.SaveChangesAsync();
            }

        }

        /// <summary>
        /// Upsert a list of variant prices in the database.
        /// </summary>
        /// <param name="variantPrices">The list of variant prices to update.</param>
        public async Task UpsertVariantPricesAsync(List<VariantPrice> variantPrices)
        {

            foreach (var variantPrice in variantPrices)
            {
                var existing = await _context.VariantPrices
                    .FirstOrDefaultAsync(x => x.VariantId == variantPrice.Variant.Id && x.LocationId == variantPrice.Location.Id);
                if (existing != null)
                {
                    existing.Price = variantPrice.Price;
                    existing.CompareAtPrice = variantPrice.CompareAtPrice;
                    existing.Currency = variantPrice.Currency;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _context.VariantPrices.Update(existing);
                }
                else
                {
                    variantPrice.UpdatedAt = DateTime.UtcNow;
                    await _context.VariantPrices.AddAsync(variantPrice);
                }
            }
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Updates a list of variant prices in the database.
        /// </summary>
        /// <param name="variantPrices">The list of variant prices to update.</param>
        public async Task UpdateVariantPriceAsync(List<VariantPrice> variantPrices)
        {
            _context.VariantPrices.UpdateRange(variantPrices);
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Updates a list of OEM variants in the database.
        /// </summary>
        /// <param name="oemVariants">The list of OEM variants to update.</param>
        public async Task UpdateOemVariantsAsync(List<OemVariant> oemVariants)
        {

            var variantIds = oemVariants.Select(x => x.VariantId).ToList();
            var oemIds = oemVariants.Select(x => x.OEMId).ToList();

            var existingRecords = await _context.OemVariants
                .Where(x => variantIds.Contains(x.VariantId) && oemIds.Contains(x.OEMId))
                .ToListAsync();

            var existingSet = new HashSet<(int VariantId, int OEMId)>(
                existingRecords.Select(x => (x.VariantId, x.OEMId))
            );

            var newItems = oemVariants
                .Where(x => !existingSet.Contains((x.VariantId, x.OEMId)))
                .ToList();

            // Log or use separately
            var alreadyExistingItems = oemVariants
                .Where(x => existingSet.Contains((x.VariantId, x.OEMId)))
                .ToList();

            // Add only new items
            if (newItems.Any())
            {
                await _context.OemVariants.AddRangeAsync(newItems);
                await _context.SaveChangesAsync();
            }
            if (alreadyExistingItems.Any())
            {
                _context.OemVariants.UpdateRange(alreadyExistingItems);
                await _context.SaveChangesAsync();
            }

            //foreach (var oemVariant in oemVariants)
            //{
            //    var existing = await _context.OemVariants
            //        .FirstOrDefaultAsync(x => x.VariantId == oemVariant.VariantId && x.OEMId == oemVariant.OEMId);
            //    if (existing == null)
            //    {
            //        await _context.OemVariants.AddAsync(oemVariant);
            //    }
            //}
            //await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Gets an existing OEM by name or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="oemName">The name of the OEM.</param>
        /// <returns>The OEM entity.</returns>
        public async Task<OEM> GetOrCreateOEMByNameAsync(string oemName)
        {

            var existingOEM = await _context.OEMs.FirstOrDefaultAsync(o => o.Name == oemName);
            if (existingOEM != null)
            {
                return existingOEM;
            }

            var newOEM = new OEM { Name = oemName };
            await _context.OEMs.AddAsync(newOEM);
            await _context.SaveChangesAsync();
            return newOEM;

        }

        // Deletion methods for full synchronization
        /// <summary>
        /// Deletes a variant and its related entities.
        /// </summary>
        /// <param name="variant">The variant to delete.</param>
        public async Task DeleteVariantAsync(List<Variant> variants)
        {
            if (variants != null && variants.Any())
            {
                _context.Variants.RemoveRange(variants);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes a product tag relationship.
        /// </summary>
        /// <param name="productTag">The product tag to delete.</param>
        public async Task DeleteProductTagAsync(List<ProductTag> productTag)
        {

            _context.ProductTags.RemoveRange(productTag);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Deletes a product collection relationship.
        /// </summary>
        /// <param name="productCollection">The product collection to delete.</param>
        public async Task DeleteProductCollectionAsync(List<ProductCollection> productCollections)
        {

            _context.ProductCollections.RemoveRange(productCollections);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Deletes a product option relationship.
        /// </summary>
        /// <param name="productOption">The product option to delete.</param>
        public async Task DeleteProductOptionAsync(List<ProductOption> productOptions)
        {
            _context.ProductOptions.RemoveRange(productOptions);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a variant option value relationship.
        /// </summary>
        /// <param name="variantOptionValue">The variant option value to delete.</param>
        public async Task DeleteVariantOptionValueAsync(List<VariantOptionValue> variantOptionValues)
        {

            _context.VariantOptionValues.RemoveRange(variantOptionValues);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Deletes an inventory level.
        /// </summary>
        /// <param name="inventoryLevel">The inventory level to delete.</param>
        public async Task DeleteInventoryLevelAsync(List<InventoryLevel> inventoryLevels)
        {

            _context.InventoryLevels.RemoveRange(inventoryLevels);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Deletes a variant price.
        /// </summary>
        /// <param name="variantPrice">The variant price to delete.</param>
        public async Task DeleteVariantPriceAsync(List<VariantPrice> variantPrices)
        {

            _context.VariantPrices.RemoveRange(variantPrices);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Deletes an OEM variant relationship.
        /// </summary>
        /// <param name="oemVariant">The OEM variant to delete.</param>
        public async Task DeleteOemVariantAsync(List<OemVariant> oemVariants)
        {

            _context.OemVariants.RemoveRange(oemVariants);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Deletes an option value.
        /// </summary>
        /// <param name="optionValue">The option value to delete.</param>
        public async Task DeleteOptionValueAsync(List<OptionValue> optionValues)
        {

            _context.OptionValues.RemoveRange(optionValues);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Gets option values for a specific product.
        /// </summary>
        /// <param name="productId">The product ID.</param>
        /// <returns>List of option values for the product.</returns>
        public async Task<List<OptionValue>> GetOptionValuesByProductIdAsync(int productId)
        {

            return await _context.OptionValues
                .Where(ov => _context.ProductOptions
                    .Where(po => po.ProductId == productId)
                    .Select(po => po.OptionId)
                    .Contains(ov.OptionId))
                .ToListAsync();

        }
        /// <summary>
        /// Gets product options for a specific product.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<List<ProductOption>> GetProductOptionsByProductIdAsync(int productId)
        {

            return await _context.ProductOptions.Where(po => po.ProductId == productId).ToListAsync();
        }

        /// <summary>
        /// Gets product tags for a specific product.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<List<ProductTag>> GetProductTagsByProductIdAsync(int productId)
        {

            return await _context.ProductTags.Where(pt => pt.ProductId == productId).ToListAsync();
        }
        /// <summary>
        /// Gets product collections for a specific product.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<List<ProductCollection>> GetProductCollectionsByProductIdAsync(int productId)
        {

            return await _context.ProductCollections
                .Include(pc => pc.Collection)
                .Where(pc => pc.ProductId == productId)
                .ToListAsync();

        }

        /// <summary>
        /// Gets variant prices by variant ID.
        /// </summary>
        /// <param name="variantId">The variant ID.</param>
        /// <returns>List of variant prices.</returns>
        public async Task<List<VariantPrice>> GetVariantPricesByVariantIdAsync(int variantId)
        {

            return await _context.VariantPrices
                .Include(vp => vp.Location)
                .Include(x => x.Variant)
                .Where(vp => vp.VariantId == variantId)
                .ToListAsync();

        }

        /// <summary>
        /// Gets OEM variants by variant ID.
        /// </summary>
        /// <param name="variantId">The variant ID.</param>
        /// <returns>List of OEM variants.</returns>
        public async Task<List<OemVariant>> GetOemVariantsByVariantIdAsync(int variantId)
        {

            return await _context.OemVariants
                .Include(ov => ov.OEM)
                .Where(ov => ov.VariantId == variantId)
                .ToListAsync();

        }

        /// <summary>
        /// Gets inventory levels by variant ID.
        /// </summary>
        /// <param name="variantId">The variant ID.</param>
        /// <returns>List of inventory levels.</returns>
        public async Task<List<InventoryLevel>> GetInventoryLevelsByVariantIdAsync(int variantId)
        {

            return await _context.InventoryLevels
                .Include(il => il.Location)
                .Include(x => x.Variant)
                .Where(il => il.VariantId == variantId)
                .ToListAsync();

        }

        /// <summary>
        /// Gets variant option values by variant ID.
        /// </summary>
        /// <param name="variantId">The variant ID.</param>
        /// <returns>List of variant option values.</returns>
        public async Task<List<VariantOptionValue>> GetVariantOptionValuesByVariantIdAsync(int variantId)
        {

            return await _context.VariantOptionValues
                .Include(vov => vov.OptionValue)
                .ThenInclude(ov => ov.Option)
                .Where(vov => vov.VariantId == variantId)
                .ToListAsync();

        }

        /// <summary>
        /// Updates an option.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public async Task UpdateOptionAsync(Option option)
        {

            _context.Options.Update(option);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Updates a tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task UpdateTagAsync(Tag tag)
        {

            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Updates a collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public async Task UpdateCollectionAsync(Collection collection)
        {

            _context.Collections.Update(collection);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Retrieves a list of options by their IDs.
        /// </summary>
        /// <param name="optionIds"></param>
        /// <returns></returns>
        public async Task<List<Option>> GetOptionsByIdsAsync(HashSet<int> optionIds)
        {

            return await _context.Options
                .Where(o => optionIds.Contains(o.Id))
                .ToListAsync();

        }

        /// <summary>
        /// Retrieves a list of tags by their IDs.
        /// </summary>
        /// <param name="tagIds"></param>
        /// <returns></returns>
        public async Task<List<Tag>> GetTagsByIdsAsync(HashSet<int> tagIds)
        {

            return await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

        }

        /// <summary>
        /// Retrieves a list of collections by their IDs.
        /// </summary>
        /// <param name="collectionIds"></param>
        /// <returns></returns>
        public async Task<List<Collection>> GetCollectionsByIdsAsync(HashSet<int> collectionIds)
        {

            return await _context.Collections
                .Where(c => collectionIds.Contains(c.Id))
                .ToListAsync();

        }

        /// <summary>
        /// // Updates option value
        /// </summary>
        /// <param name="optionValue"></param>
        /// <returns></returns>
        public async Task UpdateOptionValueAsync(OptionValue optionValue)
        {

            _context.OptionValues.Update(optionValue);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Updates product option
        /// </summary>
        /// <param name="productOption"></param>
        /// <returns></returns>
        public async Task UpdateProductOptionAsync(ProductOption productOption)
        {

            _context.ProductOptions.Update(productOption);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Adds product images
        /// </summary>
        /// <param name="productImages"></param>
        /// <returns></returns>
        public async Task AddProductImagesAsync(List<ProductImage> productImages)
        {
            if (productImages == null || !productImages.Any())
                return;

            // Build hashset of composite keys from incoming data
            var incomingProductIds = productImages
                .Select(p => p.ProductId)
                .Distinct()
                .ToList();

            // Fetch only existing composite keys from DB
            var existingKeys = await _context.ProductImages
        .Where(p => incomingProductIds.Contains(p.ProductId))
        .Select(p => new { p.ProductId, p.ImageShopifyId })
        .ToListAsync();

            // Convert to HashSet for fast lookup
            var existingKeySet = new HashSet<string>(
                existingKeys.Select(k => $"{k.ImageShopifyId}")
            );

            // Filter only non-existing records
            var uniqueImages = productImages
                .Where(p => !existingKeySet.Contains($"{p.ImageShopifyId}"))
                .ToList();

            if (uniqueImages.Any())
            {
                await _context.ProductImages.AddRangeAsync(uniqueImages);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets product images by product ID
        /// </summary>
        /// <param name="productId">The product ID to get images for</param>
        /// <returns>A list of product images for the specified product</returns>
        public async Task<List<ProductImage>> GetProductImagesByProductIdAsync(int productId)
        {
            return await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();
        }

        /// <summary>
        /// Gets OEM parts by OEM number.
        /// </summary>
        /// <param name="oemNumber">The OEM number to search for.</param>
        /// <returns>A list of OEM parts matching the OEM number.</returns>
        public async Task<List<OemVehicle>> GetOEMPartsByOEMAsync(string oemNumber)
        {
            return await _context.OemVehicles
                .Where(op => op.OEM == oemNumber && op.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<InventoryLevel> UpdateInventoryLevelAvailable(int locationId, int vaiantId, int available)
        {
            var inventoryLevel = await _context.InventoryLevels.
                Where(x => x.LocationId == locationId && x.VariantId == vaiantId).
                FirstOrDefaultAsync();
            inventoryLevel.Available = available;
            inventoryLevel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return inventoryLevel;
        }

        /// <summary>
        /// Gets all variants with their associated products.
        /// </summary>
        /// <returns>A list of variants with product information.</returns>
        public async Task<List<Variant>> GetAllVariantsWithProductsAsync(bool filter = true)
        {
            List<Variant> variants = new List<Variant>();
            IQueryable<Variant> query = _context.Variants.AsNoTracking().Include(v => v.Product)
                .Include(v => v.OemVariants)
                .ThenInclude(ov => ov.OEM).AsQueryable();

            if (filter)
            {
                query = query.Where(x => x.Product.Exact_Fit && x.Product.Is_Piece);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Fetches a Shopify order by its numeric ID using GraphQL API.
        /// This method is intentionally kept in the repository layer as it's a data-fetching operation
        /// from an external data source (Shopify GraphQL API).
        /// </summary>
        /// <param name="shopifyOrderId">The numeric Shopify order ID</param>
        /// <returns>JsonElement containing the order and fulfillment orders data, or null if not found</returns>
        public async Task<JsonElement?> FetchShopifyOrderByIdAsync(long shopifyOrderId)
        {
            // This method is intentionally empty as it will be implemented in the service layer
            // The repository pattern here is used for consistency, but the actual implementation
            // should be in ShopifyService where HTTP client configuration exists
            throw new NotImplementedException("This method should be called from IShopifyService.FetchShopifyOrderByIdAsync");
        }
    }
}