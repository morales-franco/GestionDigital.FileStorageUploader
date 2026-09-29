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
    private readonly TimeProvider _clock;
    private readonly IUploadExecutionService _executions;

    private const int BatchSize = 50;

    public UploaderHandler(
        ILogger<UploaderHandler> logger,
        IFileUploadService files,
        ICloudStorageService cloud,
        IOptions<StorageSettings> settings,
        IUploadExecutionService executions,
        TimeProvider? clock = null)
    {
        _logger = logger;
        _files = files;
        _cloud = cloud;
        _settings = settings.Value;
        _clock = clock ?? TimeProvider.System;
        _executions = executions;
    }

    public async Task HandleAsync(
        DateTime windowEnd,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = _clock.GetLocalNow().DateTime;

        if (now >= windowEnd)
            return;

        var executionId = await _executions.StartAsync(now, _settings.DefaultStorageTypeId, cancellationToken);
        long uploaded = 0;
        try
        {
            ValidateMonthsInDb(now);
            var cutoff = now.AddMonths(-_settings.MonthsInDb);
            var expected = await _files.CountAsync(cutoff, cancellationToken);
            await _executions.SetExpectedCountAsync(executionId, expected, cancellationToken);
            var storage = await _files.GetCloudStorageAsync(_settings.DefaultStorageTypeId, cancellationToken);
            _logger.LogInformation(
                "UploaderHandler [START] - ExecutionId: {ExecutionId}. CreatedDate before {Cutoff}. Expected: {Expected}",
                executionId, cutoff, expected);

            while (uploaded < expected && IsWithinWindow(windowEnd))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var files = await _files.GetBatchAsync(cutoff,
                    (int)Math.Min(BatchSize, expected - uploaded), cancellationToken);
                if (files.Count == 0) break;
                await ProcessBatchAsync(files, storage, executionId, windowEnd,
                    () => uploaded++, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var status = uploaded < expected && !IsWithinWindow(windowEnd)
                ? UploadExecutionStatus.SUCCESS_INTERRUPTED
                : UploadExecutionStatus.SUCCESS;
            await _executions.FinishAsync(executionId, status, _clock.GetLocalNow().DateTime, null, cancellationToken);
            _logger.LogInformation("UploaderHandler [END] - ExecutionId: {ExecutionId}. Status: {Status}. Uploaded: {Uploaded}/{Expected}",
                executionId, status, uploaded, expected);
        }
        catch (Exception ex)
        {
            // The host token may be cancelled. Give the audit write its own bounded lifetime.
            using var auditTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                var error = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                    ? "Uploader cancelled by service shutdown. " + ex.Message
                    : ex.ToString();
                await _executions.FinishAsync(executionId, UploadExecutionStatus.FAILED,
                    _clock.GetLocalNow().DateTime, error, auditTimeout.Token);
            }
            catch (Exception auditError)
            {
                _logger.LogError(auditError, "Could not finalize upload execution {ExecutionId}. Original error: {Error}",
                    executionId, ex.ToString());
            }
            throw;
        }
    }

    private void ValidateMonthsInDb(DateTime now)
    {
        var maxMonths = (now.Year - 1) * 12 + now.Month - 1;

        if (_settings.MonthsInDb < 0 ||
            _settings.MonthsInDb > maxMonths)
        {
            throw new InvalidOperationException(
                "StorageSettings:MonthsInDb is outside the supported date range.");
        }
    }

    private bool IsWithinWindow(DateTime windowEnd)
    {
        return _clock.GetLocalNow().DateTime < windowEnd;
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

            if (!IsWithinWindow(windowEnd))
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
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
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
