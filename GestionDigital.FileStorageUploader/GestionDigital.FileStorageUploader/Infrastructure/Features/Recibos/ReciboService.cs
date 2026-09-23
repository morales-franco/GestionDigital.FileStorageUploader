using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;
using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Recibos;

public class ReciboService : IReciboService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ReciboService> _logger;
    private readonly StorageSettings _storageSettings;
    private readonly DeleteLocalFileSettings _deleteLocalFileSettings;

    public ReciboService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ReciboService> logger,
        IOptions<StorageSettings> storageSettings,
        IOptions<DeleteLocalFileSettings> deleteLocalFileSettings)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _storageSettings = storageSettings.Value;
        _deleteLocalFileSettings = deleteLocalFileSettings.Value;
    }


    public async Task<int> CountToUploadAsync(ScheduleByEmpresa scheduleByEmpresa, CancellationToken cancellationToken)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var now = DateOnly.FromDateTime(DateTime.Now);

            if(scheduleByEmpresa.StartSyncAt > now)
            {
                return 0;
            }

            var minPeriodInLocalDb = now.AddMonths(scheduleByEmpresa.MonthsInLocalDB * -1).ToDateTime(TimeOnly.MinValue);

            return await context.Recibos.CountAsync(x => x.StorageTypeId == StorageType.LOCAL &&
                                             x.EmpresaId == scheduleByEmpresa.EmpresaId &&
                                             x.Periodo <= minPeriodInLocalDb,
                                             cancellationToken);
        }
    }

    public async Task<int> DeletePendingRecibosAsync(DateTime windowEnd, CancellationToken cancellationToken)
    {
        var lastId = 0;
        var totalRecibosToDeleted = 0;

        while (!cancellationToken.IsCancellationRequested && DateTime.Now < windowEnd)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var reciboIds = await context.ReciboLocalFiles
                    .Where(x => x.PendingDelete &&
                                x.Id > lastId)
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .Take(_deleteLocalFileSettings.BatchSize)
                    .ToListAsync(cancellationToken);

                if(!reciboIds.Any())
                {
                    break;
                }

                var totalDeleted = await context.ReciboLocalFiles
                                                .Where(x => x.PendingDelete && reciboIds.Contains(x.Id))
                                                .ExecuteDeleteAsync(cancellationToken);

                totalRecibosToDeleted += totalDeleted;
                lastId = reciboIds[^1]; //equivalente pero más performante que: reciboIds[reciboIds.Count - 1]; / reciboIds.Last();

                _logger.LogInformation(
                    "DeletePendingRecibosAsync batch deleted. LastProcessedId: {LastProcessedId}. Recibos deleted in this iteration: {DeletedInBatch}. TotalDeleted: {TotalDeleted}",
                    lastId,
                    totalDeleted,
                    totalRecibosToDeleted);
            }
        }

        if (DateTime.Now >= windowEnd)
            _logger.LogInformation("Delete window closed. No more batches will be started. TotalDeleted: {TotalDeleted}", totalRecibosToDeleted);

        _logger.LogInformation(
            "DeletePendingRecibosAsync finished. TotalDeleted: {TotalDeleted}",
            totalRecibosToDeleted);

        return totalRecibosToDeleted;
    }

    public async Task<IEnumerable<Recibo>> GetRecibosToUploadAsync(ScheduleByEmpresa scheduleByEmpresa, int batchSize, CancellationToken cancellationToken)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var now = DateOnly.FromDateTime(DateTime.Now);

            if (scheduleByEmpresa.StartSyncAt > now)
            {
                return Enumerable.Empty<Recibo>();
            }

            var minPeriodInLocalDb = now.AddMonths(scheduleByEmpresa.MonthsInLocalDB * -1).ToDateTime(TimeOnly.MinValue);

            return await context.Recibos.Where(x => x.StorageTypeId == StorageType.LOCAL &&
                                             x.EmpresaId == scheduleByEmpresa.EmpresaId &&
                                             x.Periodo <= minPeriodInLocalDb)
                                        .Include(x => x.LocalFile)
                                        .OrderBy(x => x.Id)
                                        .Take(batchSize)
                                        .ToListAsync(cancellationToken);        
        }
    }

    public async Task MarkRecibosAsUploaded(IEnumerable<Recibo> recibos, CancellationToken cancellationToken)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

            var reciboIds = recibos.Select(x => x.Id);

            await context.ReciboLocalFiles
                         .Where(x => reciboIds.Contains(x.Id))
                         .ExecuteUpdateAsync(x => 
                            x.SetProperty(f => f.PendingDelete, true)
                             .SetProperty(f => f.Timestamp, DateTime.Now),
                          cancellationToken);


            var recibosDb = await context.Recibos.Where(x => reciboIds.Contains(x.Id))
                                                 .ToListAsync(cancellationToken);

            foreach (var reciboDb in recibosDb)
            {
                var recibo = recibos.First(x => x.Id == reciboDb.Id);

                reciboDb.StorageTypeId = recibo.StorageTypeId;
                reciboDb.Timestamp = DateTime.Now;
                reciboDb.SetCloudFile(recibo.CloudFile!);
            }

            context.Recibos.UpdateRange(recibosDb);
            await context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }
    }
}
