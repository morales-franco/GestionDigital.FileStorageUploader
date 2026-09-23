namespace GestionDigital.FileStorageUploader.Core.Application.Features.DeleteLocalFiles;

public class DeleteLocalFileHandler : IDeleteLocalFileHandler
{
    private readonly ILogger<DeleteLocalFileHandler> _logger;

    public DeleteLocalFileHandler(ILogger<DeleteLocalFileHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DateTime windowEnd, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteLocalFileHandler [START]");
        _logger.LogInformation("DeleteLocalFileHandler [END]");
        return Task.CompletedTask;
    }
}

public interface IDeleteLocalFileHandler
{
    Task HandleAsync(DateTime windowEnd, CancellationToken cancellationToken);
}
