using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopifySync_BusinessLogicLayer.Service.Interface;

namespace ShopifySync_BusinessLogicLayer.Infrastructure.Job.Background
{
    public class ImportProductsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ImportProductsBackgroundService> _logger;
        private volatile bool _triggerRequested = false;
        private volatile bool _isRunning = false;
        private readonly object _lock = new object();
        DateTime ? _date;

        public ImportProductsBackgroundService(IServiceProvider serviceProvider, ILogger<ImportProductsBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public bool IsRunning => _isRunning;

        public void TriggerImport(DateTime? date)
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    _triggerRequested = true;
                    _date = date ?? null;
                    _logger.LogInformation("ImportProducts job trigger requested.");
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_triggerRequested && !_isRunning)
                {
                    lock (_lock)
                    {
                        if (_triggerRequested && !_isRunning)
                        {
                            _isRunning = true;
                            _triggerRequested = false;
                        }
                    }
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var shopifyService = scope.ServiceProvider.GetRequiredService<IShopifyService>();
                            _logger.LogInformation("Starting ImportProducts background job...");
                            await shopifyService.ImportProducts(_date);
                            _logger.LogInformation("ImportProducts background job completed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error running ImportProducts background job");
                    }
                    finally
                    {
                        _isRunning = false;
                    }
                }
                await Task.Delay(1000, stoppingToken); // Poll every second
            }
        }
    }
}