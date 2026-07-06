using PartFinderMicroServices_DataAccessLayer.Entities.RabbitMQ;
using RabbitMQ.Client;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    /// <summary>
    /// Service interface for processing INBOUND webhooks FROM Shopify via RabbitMQ
    /// Handles Product, Collection, and InventoryLevel webhook messages
    /// </summary>
    public interface IWebHookRMQService
    {
        Task<IChannel> InitializeMQ(RabbitMQSetting setting);
        Task SendDataToQueue(string jsonBody, string queueName);
        Task ReceviedData(List<string> queues);
    }
}

