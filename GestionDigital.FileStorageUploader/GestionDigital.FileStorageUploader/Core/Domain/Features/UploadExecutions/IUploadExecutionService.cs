namespace GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;

public interface IUploadExecutionService
{
    Task<long> StartAsync(DateTime startedAt, string bucket, CancellationToken cancellationToken);
    Task SetExpectedCountAsync(long id, long count, CancellationToken cancellationToken);
    Task FinishAsync(long id, string status, DateTime finishedAt, string? errorMessage, CancellationToken cancellationToken);
}
