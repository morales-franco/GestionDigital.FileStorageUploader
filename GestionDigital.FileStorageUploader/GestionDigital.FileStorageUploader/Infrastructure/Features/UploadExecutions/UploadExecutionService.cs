using GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.UploadExecutions;

public class UploadExecutionService(IDbContextFactory<ApplicationDbContext> contextFactory) : IUploadExecutionService
{
    public async Task<long> StartAsync(DateTime startedAt, string bucket, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var execution = new UploadExecution
        {
            StartedAt = startedAt, CreatedDate = DateTime.Now, TargetStorageTypeId = bucket
        };
        context.UploadExecutions.Add(execution);
        await context.SaveChangesAsync(cancellationToken);
        return execution.Id;
    }

    public async Task SetExpectedCountAsync(long id, long count, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var changed = await context.UploadExecutions
            .Where(x => x.Id == id && x.Status == UploadExecutionStatus.RUNNING)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpectedFilesCount, count), cancellationToken);
        if (changed != 1) throw new InvalidOperationException($"Running upload execution {id} not found.");
    }

    public async Task FinishAsync(long id, string status, DateTime finishedAt, string? errorMessage, CancellationToken cancellationToken)
    {
        if (status is not (UploadExecutionStatus.SUCCESS or UploadExecutionStatus.SUCCESS_INTERRUPTED or UploadExecutionStatus.FAILED))
            throw new ArgumentOutOfRangeException(nameof(status));
        var message = errorMessage is { Length: > 1000 } ? errorMessage[..1000] : errorMessage;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var changed = await context.UploadExecutions
            .Where(x => x.Id == id && x.Status == UploadExecutionStatus.RUNNING)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status)
                .SetProperty(x => x.FinishedAt, finishedAt)
                .SetProperty(x => x.ErrorMessage, message), cancellationToken);
        if (changed != 1) throw new InvalidOperationException($"Running upload execution {id} not found.");
    }
}
