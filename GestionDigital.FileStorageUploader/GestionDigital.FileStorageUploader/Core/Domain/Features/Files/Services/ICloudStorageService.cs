using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;

public interface ICloudStorageService
{
    Task<string> UploadAsync(FileEntity file, string bucket, CancellationToken cancellationToken);
}
