namespace GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas.Services;

public interface IScheduleByEmpresaExecutionService
{
    Task<ScheduleByEmpresaExecution?> CreateRunningExecutionAsync(
        long scheduleByEmpresaEmpresaId,
        int expectedCount,
        string targetStorageTypeId,
        CancellationToken cancellationToken);

    Task CompleteExecutionAsync(
        int executionId,
        int uploadedCount,
        DateTime finishedAt,
        CancellationToken cancellationToken);

    Task InterruptExecutionAsync(int executionId, int uploadedCount, DateTime finishedAt, CancellationToken cancellationToken);

    Task FailExecutionAsync(
        int executionId,
        int uploadedCount,
        string errorMessage,
        DateTime finishedAt,
        CancellationToken cancellationToken);
}
