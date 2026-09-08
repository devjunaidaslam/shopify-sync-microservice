using ShopifySync_DataAccessLayer.Entities.RabbitMQ;
using RabbitMQ.Client;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    /// <summary>
    /// Service interface for processing OUTBOUND updates TO Shopify via RabbitMQ
    /// Handles VariantPrice, VariantLocation, InventoryLevel, and FitmentSync update messages
    /// </summary>
    public interface IShopifyUpdateRMQService
    {
        Task<IChannel> InitializeMQ(RabbitMQSetting setting);
        Task SendDataToQueue(string jsonBody, string queueName);
        Task ReceviedData(List<string> queues);
    }
}

