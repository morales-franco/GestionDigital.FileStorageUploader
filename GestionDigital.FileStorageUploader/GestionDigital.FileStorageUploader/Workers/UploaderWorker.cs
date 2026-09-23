using GestionDigital.FileStorageUploader.Core.Application.Features.UploadRecibos;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Scheduling;

namespace GestionDigital.FileStorageUploader.Workers;

public class UploaderWorker : ScheduledWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UploaderWorker(ILogger<UploaderWorker> logger, IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
        : base(new WorkerSchedule(configuration.GetSection("CronJobs:UploaderWorker")
            .Get<WorkerScheduleSettings>() ?? throw new InvalidOperationException("Missing CronJobs:UploaderWorker configuration.")), logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task RunAsync(DateTime windowEnd, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IUploaderHandler>().HandleAsync(windowEnd, token);
    }
}
