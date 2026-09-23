namespace GestionDigital.FileStorageUploader.Core.Application.Features.UploadRecibos;

public class UploaderHandler : IUploaderHandler
{
    private readonly ILogger<UploaderHandler> _logger;

    public UploaderHandler(ILogger<UploaderHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DateTime windowEnd, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UploaderHandler [START]");
        _logger.LogInformation("UploaderHandler [END]");
        return Task.CompletedTask;
    }
}

public interface IUploaderHandler
{
    Task HandleAsync(DateTime windowEnd, CancellationToken cancellationToken);
}
