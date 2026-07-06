using Microsoft.Extensions.DependencyInjection;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Service.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Enum;
using System;

namespace ShopifyConnector.BackgroundJob
{
    public class WebHookRMQJob : BackgroundService
    {
        private readonly ILogger<WebHookRMQJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        public WebHookRMQJob(ILogger<WebHookRMQJob> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var scope = _serviceProvider.CreateScope();

                _logger.LogInformation("[WebHookRMQJob] Background Worker Started for Inbound Webhooks");

                // Get the service for inbound webhook processing
                var webHookRMQService = scope.ServiceProvider.GetRequiredService<IWebHookRMQService>();

                // INBOUND QUEUES - Process webhooks FROM Shopify
                List<string> inboundQueues = new List<string>()
                {
                    QueueName.ProductWebhook.ToString(),
                    QueueName.CollectionWebhook.ToString(),
                    QueueName.InventoryLevelWebhook.ToString(),
                    QueueName.OrderWebhook.ToString()
                };

                // Process inbound webhooks with WebHookRMQService
                _logger.LogInformation("[WebHookRMQJob] Starting WebHookRMQService for inbound queues: {Queues}", string.Join(", ", inboundQueues));
                await webHookRMQService.ReceviedData(inboundQueues);
                
                _logger.LogInformation("[WebHookRMQJob] Consumers started, keeping service alive...");
                
                // Keep the background service running indefinitely until cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[WebHookRMQJob] Background Worker is shutting down gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookRMQJob] Error occurred while processing inbound webhooks");
                throw;
            }
        }
    }
}
