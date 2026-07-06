using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Jobs
{
    /// <summary>
    /// Background job for cleaning up old transaction history records
    /// </summary>
    public class TransactionHistoryCleanupJob
    {
        private readonly ILogger<TransactionHistoryCleanupJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TransactionHistoryCleanupJob(
            ILogger<TransactionHistoryCleanupJob> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Executes the cleanup job to remove old transaction history records
        /// </summary>
        /// <param name="retentionDays">Number of days to retain records (default: 30)</param>
        /// <returns>Task representing the cleanup operation</returns>
        public async Task ExecuteAsync(int retentionDays = 30)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var transactionHistoryRepo = scope.ServiceProvider.GetRequiredService<ITransactionHistoryRepository>();

                try
                {
                    _logger.LogInformation("[TransactionHistoryCleanupJob] Starting cleanup of transaction history records older than {RetentionDays} days", retentionDays);

                    var deletedCount = await transactionHistoryRepo.DeleteOldRecordsAsync(retentionDays);

                    _logger.LogInformation("[TransactionHistoryCleanupJob] Successfully cleaned up {DeletedCount} old transaction history records", deletedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TransactionHistoryCleanupJob] Error during cleanup of transaction history records");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Quartz job for scheduled cleanup of transaction history records
    /// </summary>
    public class TransactionHistoryCleanupQuartzJob : IJob
    {
        private readonly ILogger<TransactionHistoryCleanupQuartzJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public TransactionHistoryCleanupQuartzJob(
            ILogger<TransactionHistoryCleanupQuartzJob> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("[TransactionHistoryCleanupQuartzJob] Starting scheduled cleanup job");

                // Get retention days from configuration or job data
                var retentionDays = _configuration.GetValue<int>("TransactionHistory:RetentionDays", 30);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var transactionHistoryRepo = scope.ServiceProvider.GetRequiredService<ITransactionHistoryRepository>();
                    
                    var deletedCount = await transactionHistoryRepo.DeleteOldRecordsAsync(retentionDays);
                    
                    _logger.LogInformation("[TransactionHistoryCleanupQuartzJob] Deleted {DeletedCount} old transaction history records", deletedCount);
                }

                _logger.LogInformation("[TransactionHistoryCleanupQuartzJob] Scheduled cleanup job completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionHistoryCleanupQuartzJob] Error during scheduled cleanup job");
                throw;
            }
        }
    }

    /// <summary>
    /// Background service for transaction history cleanup
    /// </summary>
    public class TransactionHistoryCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<TransactionHistoryCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _period = TimeSpan.FromHours(24); // Run daily

        public TransactionHistoryCleanupBackgroundService(
            ILogger<TransactionHistoryCleanupBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[TransactionHistoryCleanupBackgroundService] Starting transaction history cleanup background service");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var retentionDays = _configuration.GetValue<int>("TransactionHistory:RetentionDays", 30);
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var transactionHistoryRepo = scope.ServiceProvider.GetRequiredService<ITransactionHistoryRepository>();
                        var deletedCount = await transactionHistoryRepo.DeleteOldRecordsAsync(retentionDays);
                        _logger.LogInformation("[TransactionHistoryCleanupBackgroundService] Deleted {DeletedCount} old transaction history records", deletedCount);
                    }

                    _logger.LogInformation("[TransactionHistoryCleanupBackgroundService] Cleanup completed, waiting for next cycle");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TransactionHistoryCleanupBackgroundService] Error during cleanup cycle");
                }

                await Task.Delay(_period, stoppingToken);
            }

            _logger.LogInformation("[TransactionHistoryCleanupBackgroundService] Transaction history cleanup background service stopped");
        }
    }
}
