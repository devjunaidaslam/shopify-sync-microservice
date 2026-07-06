using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.PredikoDTO;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    /// <summary>
    /// Service for publishing order data to Prediko microservice via RabbitMQ
    /// </summary>
    public interface IPredikoOrderRMQService
    {
        /// <summary>
        /// Publishes an order message to the Prediko queue
        /// </summary>
        /// <param name="orderMessage">The enriched order message to publish</param>
        /// <returns>True if published successfully, false otherwise</returns>
        Task<bool> PublishOrderAsync(PredikoOrderMessageDTO orderMessage);
    }
}
