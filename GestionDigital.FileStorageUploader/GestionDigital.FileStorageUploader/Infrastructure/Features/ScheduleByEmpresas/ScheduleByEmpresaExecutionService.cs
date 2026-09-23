using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;
using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas.Services;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.ScheduleByEmpresas;

public class ScheduleByEmpresaExecutionService : IScheduleByEmpresaExecutionService
{
    private const string OneRunningExecutionIndexName = "UX_ScheduleByEmpresaExecutions_OneRunningByEmpresa";
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ScheduleByEmpresaExecutionService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ScheduleByEmpresaExecution?> CreateRunningExecutionAsync(
        long scheduleByEmpresaEmpresaId,
        int expectedCount,
        string targetStorageTypeId,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();

        var execution = new ScheduleByEmpresaExecution
        {
            ScheduleByEmpresaEmpresaId = scheduleByEmpresaEmpresaId,
            StartedAt = DateTime.Now,
            Status = ScheduleByEmpresaExecutionStatus.RUNNING,
            TargetStorageTypeId = targetStorageTypeId,
            ExpectedRecibosCount = expectedCount,
            UploadedRecibosCount = 0,
            CreatedDate = DateTime.Now,
        };

        context.ScheduleByEmpresaExecutions.Add(execution);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return null;
        }

        return execution;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
               && sqlException.Errors
                   .Cast<SqlError>()
                   .Any(error => error.Number is 2601 or 2627
                                 && error.Message.Contains(OneRunningExecutionIndexName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task CompleteExecutionAsync(
        int executionId,
        int uploadedCount,
        DateTime finishedAt,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();

        var affectedRows = await context.ScheduleByEmpresaExecutions
            .Where(x => x.Id == executionId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(e => e.UploadedRecibosCount, uploadedCount)
                 .SetProperty(e => e.FinishedAt, finishedAt)
                 .SetProperty(e => e.Status, ScheduleByEmpresaExecutionStatus.SUCCESS)
                 .SetProperty(e => e.ErrorMessage, (string?)null),
                cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"ScheduleByEmpresaExecution {executionId} not found.");
        }
    }

    public async Task InterruptExecutionAsync(
        int executionId,
        int uploadedCount,
        DateTime finishedAt,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();

        var affectedRows = await context.ScheduleByEmpresaExecutions
            .Where(x => x.Id == executionId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(e => e.UploadedRecibosCount, uploadedCount)
                 .SetProperty(e => e.FinishedAt, finishedAt)
                 .SetProperty(e => e.Status, ScheduleByEmpresaExecutionStatus.SUCCESS_INTERRUPTED)
                 .SetProperty(e => e.ErrorMessage, (string?)null),
                cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"ScheduleByEmpresaExecution {executionId} not found.");
        }
    }

    public async Task FailExecutionAsync(
        int executionId,
        int uploadedCount,
        string errorMessage,
        DateTime finishedAt,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();

        var affectedRows = await context.ScheduleByEmpresaExecutions
            .Where(x => x.Id == executionId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(e => e.UploadedRecibosCount, uploadedCount)
                 .SetProperty(e => e.FinishedAt, finishedAt)
                 .SetProperty(e => e.Status, ScheduleByEmpresaExecutionStatus.FAILED)
                 .SetProperty(e => e.ErrorMessage, errorMessage),
                cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"ScheduleByEmpresaExecution {executionId} not found.");
        }
    }

}
