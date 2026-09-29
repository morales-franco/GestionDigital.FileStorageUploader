using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;

public interface IFileUploadService
{
    Task<Storage> GetCloudStorageAsync(string bucket, CancellationToken cancellationToken);
    Task<IReadOnlyList<FileEntity>> GetBatchAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken);
    Task<long> CountAsync(DateTime cutoff, CancellationToken cancellationToken);
    Task MarkUploadedAsync(FileEntity file, string storageCode, string objectName, long executionId, CancellationToken cancellationToken);
}
