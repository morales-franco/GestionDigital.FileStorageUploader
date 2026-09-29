namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;

public interface ILocalFileService
{
    Task<long> DeletePendingFilesAsync(DateTime windowEnd, CancellationToken cancellationToken);
}
