using Microsoft.Extensions.DependencyInjection;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Enum;

namespace ShopifyConnector.BackgroundJob
{
    public class ShopifyUpdateRMQJob : BackgroundService
    {
        private readonly ILogger<ShopifyUpdateRMQJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        public ShopifyUpdateRMQJob(ILogger<ShopifyUpdateRMQJob> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var scope = _serviceProvider.CreateScope();

                _logger.LogInformation("[ShopifyUpdateRMQJob] Background Worker Started for Outbound Updates");

                var shopifyUpdateRMQService = scope.ServiceProvider.GetRequiredService<IShopifyUpdateRMQService>();

                List<string> outboundQueues = new List<string>()
                {
                    QueueName.VariantPriceUpdate.ToString(),
                    QueueName.VariantLocationUpdate.ToString(),
                    QueueName.InventoryLevelUpdate.ToString(),
                    QueueName.FitmentSync.ToString()
                };

                _logger.LogInformation("[ShopifyUpdateRMQJob] Starting ShopifyUpdateRMQService for outbound queues: {Queues}", string.Join(", ", outboundQueues));
                await shopifyUpdateRMQService.ReceviedData(outboundQueues);
                
                _logger.LogInformation("[ShopifyUpdateRMQJob] Consumers started, keeping service alive...");
                
                // Keep the background service running indefinitely until cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[ShopifyUpdateRMQJob] Background Worker is shutting down gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateRMQJob] Error occurred while processing outbound updates");
                throw;
            }
        }
    }
}
