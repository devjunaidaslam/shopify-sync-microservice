using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Infrastructure.Job.Background
{
    public class ShopifyRetryWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public ShopifyRetryWorker(IServiceProvider serviceProvider, ILogger<ShopifyRetryWorker> logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                using var scope = _serviceProvider.CreateScope();
                var shopifyService = scope.ServiceProvider.GetRequiredService<IShopifyService>();

                var queue = await shopifyService.GetFailedQueueAsync();

                if (queue != null && queue.Any())
                {
                    foreach (var item in queue)
                    {
                        await shopifyService.RetryFailedPageAsync(item);
                    }
                }
                }
        catch (Exception ex)
        {
            using var scope = _serviceProvider.CreateScope();
            var commonService = scope.ServiceProvider.GetRequiredService<ICommonService>();
            commonService.ErrorLogs(
                ex.StackTrace ?? "No stack trace",
                "ShopifyRetryWorker",
                1,
                ex.Message,
                ex.ToString()
            );

            // Optional: delay before retrying after failure
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry interval
            }
        }
    }
}