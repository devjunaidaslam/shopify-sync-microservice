using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using Quartz;
using System;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Infrastructure.Job.Background
{
    public class ImportProductsQuartzJob : IJob
    {
        private readonly IShopifyService _shopifyService;
        private readonly ILogger<ImportProductsQuartzJob> _logger;
        private readonly IConfiguration _configuration;

        public ImportProductsQuartzJob(IShopifyService shopifyService, ILogger<ImportProductsQuartzJob> logger, IConfiguration configuration)
        {
            _shopifyService = shopifyService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var enabled = _configuration.GetValue<bool>("Quartz:ImportShopifyJob:Enabled");
            if (!enabled)
            {
                _logger.LogInformation("Quartz ImportJob is disabled in configuration.");
                return;
            }
            try
            {
                var date = _configuration.GetValue<DateTime>("Quartz:ImportShopifyJob:Date");
                _logger.LogInformation("Quartz ImportJob started.");
                await _shopifyService.ImportProducts(date);
                _logger.LogInformation("Quartz ImportJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running Quartz ImportJob");
            }
        }
    }
}