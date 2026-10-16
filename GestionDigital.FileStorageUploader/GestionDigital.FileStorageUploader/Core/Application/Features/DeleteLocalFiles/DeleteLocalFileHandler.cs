using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;

namespace GestionDigital.FileStorageUploader.Core.Application.Features.DeleteLocalFiles;

public class DeleteLocalFileHandler : IDeleteLocalFileHandler
{
    private readonly ILogger<DeleteLocalFileHandler> _logger;
    private readonly ILocalFileService _localFileService;

    public DeleteLocalFileHandler(ILogger<DeleteLocalFileHandler> logger, ILocalFileService localFileService)
    {
        _logger = logger;
        _localFileService = localFileService;
    }

    public async Task HandleAsync(DateTime windowEnd, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteLocalFileHandler [START]");
        var totalDeleted = await _localFileService.DeletePendingFilesAsync(windowEnd, cancellationToken);
        _logger.LogInformation("DeleteLocalFileHandler [END] - Total local files deleted: {Total}", totalDeleted);
    }
}

public interface IDeleteLocalFileHandler
{
    Task HandleAsync(DateTime windowEnd, CancellationToken cancellationToken);
}
