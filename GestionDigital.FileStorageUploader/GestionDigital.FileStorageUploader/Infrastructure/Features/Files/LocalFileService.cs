using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Files;

public class LocalFileService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IOptions<DeleteLocalFileSettings> settings,
    ILogger<LocalFileService> logger) : ILocalFileService
{
    public async Task<long> DeletePendingFilesAsync(DateTime windowEnd, CancellationToken cancellationToken)
    {
        var batchSize = settings.Value.BatchSize;
        if (batchSize <= 0)
            throw new InvalidOperationException("DeleteLocalFileSettings:BatchSize must be greater than zero.");

        long lastId = 0;
        long totalDeleted = 0;

        while (!cancellationToken.IsCancellationRequested && DateTime.Now < windowEnd)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var fileIds = await context.LocalFiles
                .Where(file => file.PendingDelete && file.Id > lastId)
                .OrderBy(file => file.Id)
                .Select(file => file.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (fileIds.Count == 0)
                break;

            // Do not start a delete if the selection consumed the rest of the window.
            if (DateTime.Now >= windowEnd)
                break;

            var deletedInBatch = await context.LocalFiles
                .Where(file => file.PendingDelete && fileIds.Contains(file.Id))
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deletedInBatch;
            lastId = fileIds[^1];
            logger.LogInformation(
                "Local files batch deleted. LastProcessedId: {LastProcessedId}. DeletedInBatch: {DeletedInBatch}. TotalDeleted: {TotalDeleted}",
                lastId, deletedInBatch, totalDeleted);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (DateTime.Now >= windowEnd)
            logger.LogInformation("Delete window closed. No more batches will be started. TotalDeleted: {TotalDeleted}", totalDeleted);

        logger.LogInformation("DeletePendingFilesAsync finished. TotalDeleted: {TotalDeleted}", totalDeleted);
        return totalDeleted;
    }
}
