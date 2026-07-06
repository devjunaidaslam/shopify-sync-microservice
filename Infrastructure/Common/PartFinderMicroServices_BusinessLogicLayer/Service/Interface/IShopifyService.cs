using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System.Text.Json;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IShopifyService
    {
        Task ImportCollectionsAsync(int page);
        Task ImportLocationsAsync();
        Task ImportVendorsAsync();
        Task ImportAllProductsAndChildrenAsync(DateTime? updatedAfter);
        Task<Response> GetHistoryStatus();
        Task<Response> GetHistoryStatusPaginated(HistoryInventoryFilterDto filterDto);
        Task ImportProducts(DateTime? updatedAfter);
        Task<string> ImportProductByIdAsync(long productId);
        Task<List<InventoryLevel>> AddLocationAndSetInventoryLevelAsync(JsonElement node, Variant variant, JsonElement inventoryLevelsNode);
        Task RetryFailedPageAsync(ShopifyDataQueue item);
        Task<List<ShopifyDataQueue>> GetFailedQueueAsync();
        Task<Product> UpdateProductAsync(Product existingProduct);
        Task<Response> ImportProductImagesAsync(DateTime? updatedAfter = null);
        Task<JsonElement?> FetchShopifyOrderByIdAsync(long shopifyOrderId);
        
        // RabbitMQ Queue Operations
        /// <summary>
        /// Sends webhook data to RabbitMQ queue for processing
        /// </summary>
        Task SendWebhookToQueueAsync(string jsonBody, string queueName);
        
        /// <summary>
        /// Sends update data to RabbitMQ queue for processing
        /// </summary>
        Task SendUpdateToQueueAsync(string jsonBody, string queueName);
        Task<string> ImportProductFromShopifyAsync(string productId);
    }
}
