using GestionDigital.FileStorageUploader.Core.Domain.Features.Files;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Files;

public class FileUploadService(IDbContextFactory<ApplicationDbContext> contextFactory) : IFileUploadService
{
    public async Task<Storage> GetCloudStorageAsync(string bucket, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Storages.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StorageType == Storage.Cloud && x.BucketCloudId == bucket, cancellationToken)
            ?? throw new InvalidOperationException($"No cloud storage configured for bucket '{bucket}'.");
    }

    public async Task<IReadOnlyList<FileEntity>> GetBatchAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Files.AsNoTracking()
            .Where(x => x.StorageId == Storage.Local && x.CreatedDate < cutoff
                && x.LocalFile != null && !x.LocalFile.PendingDelete)
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.Id)
            .Include(x => x.LocalFile)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkUploadedAsync(FileEntity file, string storageCode, string objectName, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.Now;

        // Reject changes made by the API while the content was being uploaded.
        var updated = await context.Files
            .Where(x => x.Id == file.Id && x.StorageId == Storage.Local && x.Timestamp == file.Timestamp)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StorageId, storageCode)
                .SetProperty(x => x.Timestamp, now), cancellationToken);
        if (updated != 1)
            throw new DbUpdateConcurrencyException($"File {file.Id} changed during upload.");

        var localTimestamp = file.LocalFile!.Timestamp;
        var marked = await context.LocalFiles
            .Where(x => x.Id == file.Id && !x.PendingDelete && x.Timestamp == localTimestamp)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.PendingDelete, true)
                .SetProperty(x => x.Timestamp, now), cancellationToken);
        if (marked != 1)
            throw new DbUpdateConcurrencyException($"Local file {file.Id} changed during upload.");

        var cloudFile = await context.CloudFiles.AsTracking().SingleOrDefaultAsync(x => x.Id == file.Id, cancellationToken);
        if (cloudFile is null)
        {
            context.CloudFiles.Add(new CloudFile
            {
                Id = file.Id, FileCloudId = objectName, CreatedDate = now, Timestamp = now
            });
        }
        else
        {
            cloudFile.FileCloudId = objectName;
            cloudFile.Timestamp = now;
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
