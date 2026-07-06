using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PartFinderMicroServices_BusinessLogicLayer.Jobs;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Enum;

namespace PartFinderMicroServices_BusinessLogicLayer.Infrastructure.Job.Background
{
    public enum FitmentJobType
    {
        Preprocess,
        Finalize,
        ProcessAll
    }

    public record FitmentJobRequest(long ImportFitmentId, FitmentJobType JobType);

    public interface IFitmentBackgroundQueue
    {
        void Enqueue(FitmentJobRequest request);
        ChannelReader<FitmentJobRequest> Reader { get; }
    }

    public class FitmentBackgroundQueue : IFitmentBackgroundQueue
    {
        private readonly Channel<FitmentJobRequest> _channel;

        public FitmentBackgroundQueue()
        {
            _channel = Channel.CreateUnbounded<FitmentJobRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        public ChannelReader<FitmentJobRequest> Reader => _channel.Reader;

        public void Enqueue(FitmentJobRequest request)
        {
            if (!_channel.Writer.TryWrite(request))
            {
                throw new InvalidOperationException("Failed to enqueue fitment job request.");
            }
        }
    }

    public class FitmentBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FitmentBackgroundService> _logger;
        private readonly IFitmentBackgroundQueue _queue;

        public FitmentBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<FitmentBackgroundService> logger,
            IFitmentBackgroundQueue queue)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FitmentBackgroundService started.");

            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var importRepo = scope.ServiceProvider.GetRequiredService<IImportFitmentRepository>();
                    var preprocessJob = scope.ServiceProvider.GetRequiredService<PreprocessFitmentJob>();
                    var finalizeJob = scope.ServiceProvider.GetRequiredService<FinalizeFitmentJob>();

                    switch (job.JobType)
                    {
                        case FitmentJobType.Preprocess:
                            await SetStatusAsync(importRepo, job.ImportFitmentId, ImportFitmentStatuses.Preprocessing);
                            await preprocessJob.ExecuteAsync(job.ImportFitmentId);
                            break;

                        case FitmentJobType.Finalize:
                            await SetStatusAsync(importRepo, job.ImportFitmentId, ImportFitmentStatuses.Finalizing);
                            await finalizeJob.ExecuteAsync(job.ImportFitmentId);
                            break;

                        case FitmentJobType.ProcessAll:
                            await SetStatusAsync(importRepo, job.ImportFitmentId, ImportFitmentStatuses.Preprocessing);
                            await preprocessJob.ExecuteAsync(job.ImportFitmentId);
                            // If preprocessing succeeded, mark as Finalizing and run finalize
                            await SetStatusAsync(importRepo, job.ImportFitmentId, ImportFitmentStatuses.Finalizing);
                            await finalizeJob.ExecuteAsync(job.ImportFitmentId);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing fitment background job: {Job}", job);
                    // Best-effort: try to mark the import as Error
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var importRepo = scope.ServiceProvider.GetRequiredService<IImportFitmentRepository>();
                        var import = await importRepo.GetImportFitmentDetailsById(job.ImportFitmentId);
                        if (import != null)
                        {
                            import.Status = ImportFitmentStatuses.Error;
                            import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                            await importRepo.UpdateImportFitment(import);
                        }
                    }
                    catch (Exception inner)
                    {
                        _logger.LogError(inner, "Failed to set import status to Error for {ImportFitmentId}", job.ImportFitmentId);
                    }
                }
            }
        }

        private static async Task SetStatusAsync(IImportFitmentRepository importRepo, long importFitmentId, string status)
        {
            var import = await importRepo.GetImportFitmentDetailsById(importFitmentId);
            if (import != null)
            {
                import.Status = status;
                import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                await importRepo.UpdateImportFitment(import);
            }
        }
    }
}
