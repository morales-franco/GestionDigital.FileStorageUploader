using System.Globalization;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;
using Google.Cloud.Storage.V1;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Files;

public class CloudStorageService(
    StorageClient storageClient,
    IConfiguration configuration,
    ILogger<CloudStorageService> logger) : ICloudStorageService
{
    public async Task<string> UploadAsync(FileEntity file, string bucket, CancellationToken cancellationToken)
    {
        var content = file.LocalFile?.Content
            ?? throw new InvalidOperationException($"File {file.Id} has no local content.");
        var retries = Math.Max(0, configuration.GetValue("CloudStorageSettings:UploadRetries", 3));
        var retryDelay = TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("CloudStorageSettings:UploadRetryDelayInSeconds", 10)));

        // Generate once so all retries of this upload use the same object name.
        var objectName = $"{file.Id.ToString("D10", CultureInfo.InvariantCulture)}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        if (objectName.Length > 50)
            throw new InvalidOperationException($"Object name for file {file.Id} exceeds the 50-character FileCloudId limit.");

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var stream = new MemoryStream(content, writable: false);
                await storageClient.UploadObjectAsync(bucket, objectName, file.ContentType, stream,
                    cancellationToken: cancellationToken);
                return objectName;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < retries)
            {
                logger.LogWarning(ex, "Upload failed for file {FileId}. Retry {Attempt}/{Retries} in {DelaySeconds} seconds.",
                    file.Id, attempt + 1, retries, retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }
}
