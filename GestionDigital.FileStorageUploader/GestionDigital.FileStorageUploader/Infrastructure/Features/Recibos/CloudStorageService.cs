using CSharpFunctionalExtensions;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes.Services;
using Google.Cloud.Storage.V1;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Recibos;

public class CloudStorageService : ICloudStorageService
{
    private readonly ILogger<CloudStorageService> _logger;
    private readonly StorageClient _storageClient;
    private readonly IStorageTypeService _storageTypeService;
    private readonly int _uploadRetries;
    private readonly TimeSpan _uploadRetryDelay;

    public CloudStorageService(ILogger<CloudStorageService> logger,
        StorageClient storageClient,
        IStorageTypeService storageTypeService,
        IConfiguration configuration)
    {
        _logger = logger;
        _storageClient = storageClient;
        _storageTypeService = storageTypeService;
        _uploadRetries = Math.Max(0, configuration.GetValue("CloudStorageSettings:UploadRetries", 3));
        _uploadRetryDelay = TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("CloudStorageSettings:UploadRetryDelayInSeconds", 10)));
    }

    public async Task<Result<ReciboCloudIdentifier>> UploadAsync(Recibo recibo, CancellationToken cancellationToken)
    {
        var storageType = await _storageTypeService.GetByCodeAsync(recibo.StorageTypeId);

        if (storageType is null || string.IsNullOrEmpty(storageType.BucketCloudId))
        {
            return Result.Failure<ReciboCloudIdentifier>(
                $"BucketCloudId not found for storage type '{recibo.StorageTypeId}'");
        }

        var bucketId = storageType.BucketCloudId;
        var maxAttempts = _uploadRetries + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await UploadCoreAsync(recibo, bucketId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Error subiendo recibo-id {ReciboId}. Reintento {RetryAttempt}/{UploadRetries} en {RetryDelaySeconds} segundos.",
                    recibo.Id,
                    attempt,
                    _uploadRetries,
                    _uploadRetryDelay.TotalSeconds);

                if (_uploadRetryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_uploadRetryDelay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error subiendo recibo-id {recibo.Id}");
                return Result.Failure<ReciboCloudIdentifier>("Error");
            }
        }

        return Result.Failure<ReciboCloudIdentifier>("Error");
    }

    private async Task<ReciboCloudIdentifier> UploadCoreAsync(
        Recibo recibo,
        string bucketId,
        CancellationToken cancellationToken)
    {
        var contenidoOriginalTask = this.UpdateFileAsync(
            bucketId,
            $"{recibo.Id.ToString().PadLeft(10, '0')}_Original_{recibo.EmpresaId}.pdf",
            "application/pdf",
            recibo?.LocalFile?.ContenidoOriginal,
            cancellationToken);

        var contenidoFirmadoEmpleadoTask = this.UpdateFileAsync(
            bucketId,
            $"{recibo!.Id.ToString().PadLeft(10, '0')}_Empleado_{recibo.EmpresaId}.pdf.enc",
            "application/octet-stream",
            recibo?.LocalFile?.ContenidoFirmadoEmpleado,
            cancellationToken);

        var contenidoFirmadoEmpleadorTask = this.UpdateFileAsync(
            bucketId,
            $"{recibo!.Id.ToString().PadLeft(10, '0')}_Empleador_{recibo.EmpresaId}.pdf.enc",
            "application/octet-stream",
            recibo?.LocalFile?.ContenidoFirmadoEmpleador,
            cancellationToken);

        await Task.WhenAll(contenidoOriginalTask, contenidoFirmadoEmpleadorTask, contenidoFirmadoEmpleadoTask);

        return new ReciboCloudIdentifier(
             ContenidoOriginalId: contenidoOriginalTask.Result,
            ContenidoFirmadoEmpleadorId: contenidoFirmadoEmpleadorTask.Result,
            ContenidoFirmadoEmpleadoId: contenidoFirmadoEmpleadoTask.Result
            );
    }

    private async Task<string?> UpdateFileAsync(
        string bucketId,
        string cloudFileId,
        string contentType,
        byte[]? contentFile,
        CancellationToken cancellationToken)
    {
        if (contentFile is null) return null ;

        using var contentStream = new MemoryStream(contentFile);
        await _storageClient.UploadObjectAsync(bucketId, cloudFileId, contentType, contentStream, cancellationToken: cancellationToken);

        return cloudFileId;
    }
}
