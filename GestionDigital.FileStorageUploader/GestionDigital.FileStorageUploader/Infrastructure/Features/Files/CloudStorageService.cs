using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        // Stable for retries, different for modified content, and fits FileCloudId varchar(50).
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(file.Id.ToString(CultureInfo.InvariantCulture) + "\n" + file.ContentType + "\n"));
        hash.AppendData(content);
        var objectName = "files/" + Convert.ToBase64String(hash.GetHashAndReset()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
