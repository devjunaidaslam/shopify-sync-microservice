using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IShopifyRepository
    {
        Task AddVariants(List<Variant> variants);
        Task AddVariantPriceAsync(List<VariantPrice> variantPrices);
        Task AddInventoryLevelsAsync(List<InventoryLevel> inventoryLevels);
        Task UpdateInventoryLevelsAsync(List<InventoryLevel> inventoryLevels);

        Task<int> AddHistoryInventory(HistoryInventory historyInventory);

        Task UpdateHistoryInventory(int historyId,HistoryInventory historyInventory);

        Task StatusUpdateHistoryInventory(int historyId, HistoryInventory historyInventory);

        Task AddVariantOptionValueAsync(List<VariantOptionValue> VariantOptionValues);

        Task AddProductOptionAsync(List<ProductOption> productOptions);

        Task AddProductCollectionsAsync(List<ProductCollection> productCollections );

        Task AddProductTagsAsync(List<ProductTag> productTags );

        Task AddProductsAsync(List<Product> products);

        Task AddOptionsAsync(List<Option> options);
        Task AddOptionValuesAsync(List<OptionValue> optionValues);
        Task AddCollectionsAsync(List<Collection> collections);
        Task AddTagsAsync(List<Tag> tags);
        Task AddLocationsAsync(List<Location> locations);
        Task UpdateVariantPriceAsync(List<VariantPrice> variantPrices);

        Task AddVendorsAsync(List<Vendor> vendors);
        Task<Vendor> GetOrCreateVendorByNameAsync(string vendorName);

        Task<HistoryInventory> GetHistoryStatus();
        Task<(int TotalCount, List<HistoryInventoryStatusDto> Data)> GetHistoryStatusPaginated(HistoryInventoryFilterDto filterDto);

        Task<ProductType> GetOrCreateProductTypeByNameAsync(string productTypeName);

        Task<Product> GetProductByShopifyIdAsync(string shopifyProductId);
        Task UpdateProductAsync(Product existingProduct);

        Task<List<Variant>> GetVariantsByProductIdAsync(int productId);
        Task<Variant?> GetVariantByIdAsync(int variantId);

        Task<List<Location>> GetLocationsByShopifyIdsAsync(List<string> locationShopifyId);
        Task<Location> GetLocationsByShopifyIdAsync(string locationShopifyId);

        Task<Variant?> GetVariantByShopifyIdAsync(string variantShopifyId);

        Task AddShopifyQueueAsync(ShopifyDataQueue shopifyDataQueue);
        Task<ShopifyDataQueue> GetProductFromQueueAsync(string productId);
        Task<List<ShopifyDataQueue>> GetFailedQueueAsync();
        Task UpdateShopifyQueueAsync(ShopifyDataQueue shopifyDataQueue, bool isSuccessfull);
        
        Task UpdateVariantAsync(Variant variant);

        Task<List<Product>> GetProductsByShopifyIds(string shopifyId);

        Task AddVariantPricesAsync(List<VariantPrice> variantPrices);

        Task AddOemVariantsAsync(List<OemVariant> oemVariants);

        Task UpsertVariantPricesAsync(List<VariantPrice> variantPrices);

        Task UpdateOemVariantsAsync(List<OemVariant> oemVariants);

        Task<OEM> GetOrCreateOEMByNameAsync(string oemName);
        Task AddProductImagesAsync(List<ProductImage> productImages);
        Task<List<ProductImage>> GetProductImagesByProductIdAsync(int productId);

        // Deletion methods for full synchronization
        Task DeleteVariantAsync(List<Variant> variants);
        Task DeleteProductTagAsync(List<ProductTag> productTag);
        Task DeleteProductCollectionAsync( List<ProductCollection> productCollection);
        Task DeleteProductOptionAsync(List<ProductOption> productOption);
        Task DeleteVariantOptionValueAsync(List<VariantOptionValue> variantOptionValue);
        Task DeleteInventoryLevelAsync(List<InventoryLevel> inventoryLevels);
        Task DeleteVariantPriceAsync(List<VariantPrice> variantPrices);
        Task DeleteOemVariantAsync(List<OemVariant> oemVariants);
        Task DeleteOptionValueAsync(List<OptionValue> optionValues);

        Task<List<ProductOption>> GetProductOptionsByProductIdAsync(int productId);
        Task<List<OptionValue>> GetOptionValuesByProductIdAsync(int productId);
        Task<List<ProductTag>> GetProductTagsByProductIdAsync(int productId);
        Task<List<ProductCollection>> GetProductCollectionsByProductIdAsync(int productId);
        
        // Additional methods for full synchronization
        Task<List<VariantPrice>> GetVariantPricesByVariantIdAsync(int variantId);
        Task<List<OemVariant>> GetOemVariantsByVariantIdAsync(int variantId);
        Task<List<InventoryLevel>> GetInventoryLevelsByVariantIdAsync(int variantId);
        Task<List<VariantOptionValue>> GetVariantOptionValuesByVariantIdAsync(int variantId);
        Task<Collection> GetCollectionByShopifyId(string shopifyId);
        
        // Additional methods for updating entities
        Task UpdateOptionAsync(Option option);
        Task UpdateTagAsync(Tag tag);
        Task UpdateCollectionAsync(Collection collection);
        Task UpdateOptionValueAsync(OptionValue optionValue);
        Task UpdateProductOptionAsync(ProductOption productOption);
        
        // Additional methods for getting entities by IDs
        Task<List<Option>> GetOptionsByIdsAsync(HashSet<int> optionIds);
        Task<List<Tag>> GetTagsByIdsAsync(HashSet<int> tagIds);
        Task<List<Collection>> GetCollectionsByIdsAsync(HashSet<int> collectionIds);
        
        // Fitment-related methods
        Task<List<OemVehicle>> GetOEMPartsByOEMAsync(string oemNumber);
        Task<InventoryLevel> UpdateInventoryLevelAvailable(int locationId, int vaiantId,int available);

        // New methods for fitment upsert functionality
        Task<List<Variant>> GetAllVariantsWithProductsAsync(bool filter = true);

        // Order import from Shopify GraphQL
        Task<System.Text.Json.JsonElement?> FetchShopifyOrderByIdAsync(long shopifyOrderId);
    }
}