using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using Microsoft.Extensions.Options;
using GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Core.Application.Features.UploadFiles;

public class UploaderHandler : IUploaderHandler
{
    private readonly ILogger<UploaderHandler> _logger;
    private readonly IFileUploadService _files;
    private readonly ICloudStorageService _cloud;
    private readonly StorageSettings _settings;
    private readonly IUploadExecutionService _executions;

    private const int BatchSize = 50;

    public UploaderHandler(
        ILogger<UploaderHandler> logger,
        IFileUploadService files,
        ICloudStorageService cloud,
        IOptions<StorageSettings> settings,
        IUploadExecutionService executions)
    {
        _logger = logger;
        _files = files;
        _cloud = cloud;
        _settings = settings.Value;
        _executions = executions;
    }

    public async Task HandleAsync(
        DateTime windowEnd,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;

        if (now >= windowEnd)
            return;

        var executionId = await _executions.StartAsync(now, _settings.DefaultStorageTypeId, cancellationToken);
        long uploaded = 0;
        try
        {
            var cutoff = now.AddMonths(-_settings.MonthsInDb);
            var expected = await _files.CountAsync(cutoff, cancellationToken);
            await _executions.SetExpectedCountAsync(executionId, expected, cancellationToken);
            var storage = await _files.GetCloudStorageAsync(_settings.DefaultStorageTypeId, cancellationToken);
            _logger.LogInformation(
                "UploaderHandler [START] - ExecutionId: {ExecutionId}. CreatedDate before {Cutoff}. Expected: {Expected}",
                executionId, cutoff, expected);

            while (uploaded < expected && DateTime.Now < windowEnd)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var files = await _files.GetBatchAsync(cutoff,
                    (int)Math.Min(BatchSize, expected - uploaded), cancellationToken);
                if (files.Count == 0) break;
                await ProcessBatchAsync(files, storage, executionId, windowEnd,
                    () => uploaded++, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var status = uploaded < expected && DateTime.Now >= windowEnd
                ? UploadExecutionStatus.SUCCESS_INTERRUPTED
                : UploadExecutionStatus.SUCCESS;
            await _executions.FinishAsync(executionId, status, DateTime.Now, null, cancellationToken);
            _logger.LogInformation("UploaderHandler [END] - ExecutionId: {ExecutionId}. Status: {Status}. Uploaded: {Uploaded}/{Expected}",
                executionId, status, uploaded, expected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload execution {ExecutionId} failed.", executionId);
            await _executions.FinishAsync(executionId, UploadExecutionStatus.FAILED,
                DateTime.Now, ex.ToString(), CancellationToken.None);
            throw;
        }
    }

    private async Task ProcessBatchAsync(
        IReadOnlyList<FileEntity> files,
        Storage storage,
        long executionId,
        DateTime windowEnd,
        Action onUploaded,
        CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTime.Now >= windowEnd)
                break;

            await UploadFileAsync(
                file,
                storage,
                executionId,
                cancellationToken);

            onUploaded();
        }

    }

    private async Task UploadFileAsync(
        FileEntity file,
        Storage storage,
        long executionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var objectName = await _cloud.UploadAsync(
                file,
                storage.BucketCloudId!,
                cancellationToken);

            // Finish persisting an uploaded file even if the scheduling window just closed.
            await _files.MarkUploadedAsync(
                file,
                storage.Code,
                objectName,
                executionId,
                cancellationToken);

            _logger.LogInformation(
                "File {FileId} moved to cloud.",
                file.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Upload stopped at file {FileId}.",
                file.Id);

            throw;
        }
    }
}

public interface IUploaderHandler
{
    Task HandleAsync(
        DateTime windowEnd,
        CancellationToken cancellationToken);
}
