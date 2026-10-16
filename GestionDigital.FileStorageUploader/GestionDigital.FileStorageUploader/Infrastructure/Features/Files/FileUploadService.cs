using GestionDigital.FileStorageUploader.Core.Domain.Features.Files;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;
using GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Files;

public class FileUploadService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IOptions<StorageSettings> settings) : IFileUploadService
{
    private readonly long[] _whiteListFilesIds = settings.Value.WhiteListFilesIds;

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
        var query = context.Files.AsNoTracking().Where(x => x.StorageId == Storage.Local && x.CreatedDate.Date <= cutoff.Date
            && x.LocalFile != null && !x.LocalFile.PendingDelete);

        if (_whiteListFilesIds.Length > 0)
            query = query.Where(x => _whiteListFilesIds.Contains(x.Id));

        return await query
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.Id)
            .Include(x => x.LocalFile)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CountAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Files.AsNoTracking().Where(x => x.StorageId == Storage.Local && x.CreatedDate.Date <= cutoff.Date
            && x.LocalFile != null && !x.LocalFile.PendingDelete);

        if (_whiteListFilesIds.Length > 0)
            query = query.Where(x => _whiteListFilesIds.Contains(x.Id));

        return await query.LongCountAsync(cancellationToken);
    }

    public async Task MarkUploadedAsync(FileEntity file, string storageCode, string objectName, long executionId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.Now;

        var updated = await context.Files
            .Where(x => x.Id == file.Id && x.StorageId == Storage.Local)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StorageId, storageCode)
                .SetProperty(x => x.Timestamp, now), cancellationToken);
        if (updated != 1)
            throw new DbUpdateConcurrencyException($"File {file.Id} changed during upload.");

        var marked = await context.LocalFiles
            .Where(x => x.Id == file.Id && !x.PendingDelete)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.PendingDelete, true)
                .SetProperty(x => x.Timestamp, now), cancellationToken);
        if (marked != 1)
            throw new DbUpdateConcurrencyException($"Local file {file.Id} changed during upload.");

        if (await context.CloudFiles.AnyAsync(x => x.Id == file.Id, cancellationToken))
            throw new InvalidOperationException($"Cloud file {file.Id} already exists.");

        context.CloudFiles.Add(new CloudFile
        {
            Id = file.Id, FileCloudId = objectName, CreatedDate = now, Timestamp = now
        });
        await context.SaveChangesAsync(cancellationToken);
        var audited = await context.UploadExecutions
            .Where(x => x.Id == executionId && x.Status == UploadExecutionStatus.RUNNING)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UploadedFilesCount, x => x.UploadedFilesCount + 1),
                cancellationToken);
        if (audited != 1)
            throw new InvalidOperationException($"Running upload execution {executionId} not found.");
        await transaction.CommitAsync(cancellationToken);
    }
}
