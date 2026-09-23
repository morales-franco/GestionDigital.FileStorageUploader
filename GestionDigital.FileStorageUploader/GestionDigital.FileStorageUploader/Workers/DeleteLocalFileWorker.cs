using GestionDigital.FileStorageUploader.Core.Application.Features.DeleteLocalFiles;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Scheduling;

namespace GestionDigital.FileStorageUploader.Workers;

public class DeleteLocalFileWorker : ScheduledWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DeleteLocalFileWorker(ILogger<DeleteLocalFileWorker> logger, IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
        : base(new WorkerSchedule(configuration.GetSection("CronJobs:DeleteWorker")
            .Get<WorkerScheduleSettings>() ?? throw new InvalidOperationException("Missing CronJobs:DeleteWorker configuration.")), logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task RunAsync(DateTime windowEnd, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDeleteLocalFileHandler>().HandleAsync(windowEnd, token);
    }
}
