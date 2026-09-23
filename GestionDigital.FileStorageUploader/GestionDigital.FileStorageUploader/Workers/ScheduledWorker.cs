using GestionDigital.FileStorageUploader.Core.Domain.Features.Scheduling;

namespace GestionDigital.FileStorageUploader.Workers;

public abstract class ScheduledWorker : BackgroundService
{
    private readonly WorkerSchedule _schedule;
    private readonly ILogger _logger;

    protected ScheduledWorker(WorkerSchedule schedule, ILogger logger)
    {
        _schedule = schedule;
        _logger = logger;
    }

    protected abstract Task RunAsync(DateTime windowEnd, CancellationToken token);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_schedule.Enabled)
        {
            _logger.LogInformation("{Worker} is disabled.", GetType().Name);
            return;
        }
        DateTime? lastExecutedWindowStart = null;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var window = _schedule.Next(DateTime.Now, lastExecutedWindowStart);
                _logger.LogInformation($"{GetType().Name} next window: {window.Start.ToString("dd/MM/yyyy HH:mm:ss")} - {window.End.ToString("dd/MM/yyyy HH:mm:ss")}");
                var delay = window.Start - DateTime.Now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation($"[WAITING] {GetType().Name} will wait {delay} until {window.Start.ToString("dd/MM/yyyy HH:mm:ss")}");
                    await Task.Delay(delay, stoppingToken);
                }

                lastExecutedWindowStart = window.Start;
                if (DateTime.Now >= window.End)
                {
                    _logger.LogInformation($"[SKIPPED] {GetType().Name} skipped window: {window.Start.ToString("dd/MM/yyyy HH:mm:ss")} - {window.End.ToString("dd/MM/yyyy HH:mm:ss")}");
                    continue;
                }

                _logger.LogInformation("[EXECUTING] {Worker}", GetType().Name);
                try
                {
                    await RunAsync(window.End, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing {Worker}.", GetType().Name);
                }
                _logger.LogInformation("[FINISHED] {Worker}", GetType().Name);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
