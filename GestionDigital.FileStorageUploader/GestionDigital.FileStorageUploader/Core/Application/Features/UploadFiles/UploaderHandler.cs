using GestionDigital.FileStorageUploader.Core.Domain.Features.Files.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using Microsoft.Extensions.Options;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Core.Application.Features.UploadFiles;

public class UploaderHandler : IUploaderHandler
{
    private readonly ILogger<UploaderHandler> _logger;
    private readonly IFileUploadService _files;
    private readonly ICloudStorageService _cloud;
    private readonly StorageSettings _settings;
    private readonly TimeProvider _clock;

    private const int BatchSize = 50;

    public UploaderHandler(
        ILogger<UploaderHandler> logger,
        IFileUploadService files,
        ICloudStorageService cloud,
        IOptions<StorageSettings> settings,
        TimeProvider? clock = null)
    {
        _logger = logger;
        _files = files;
        _cloud = cloud;
        _settings = settings.Value;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task HandleAsync(
        DateTime windowEnd,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = _clock.GetLocalNow().DateTime;

        if (now >= windowEnd)
            return;

        ValidateMonthsInDb(now);

        var cutoff = now.AddMonths(-_settings.MonthsInDb);

        var storage = await _files.GetCloudStorageAsync(
            _settings.DefaultStorageTypeId,
            cancellationToken);

        long uploaded = 0;

        _logger.LogInformation(
            "UploaderHandler [START] - CreatedDate before {Cutoff}. MonthsInDb: {MonthsInDb}",
            cutoff,
            _settings.MonthsInDb);

        while (IsWithinWindow(windowEnd))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var files = await _files.GetBatchAsync(
                cutoff,
                BatchSize,
                cancellationToken);

            if (files.Count == 0)
                break;

            uploaded += await ProcessBatchAsync(
                files,
                storage,
                windowEnd,
                cancellationToken);
        }

        _logger.LogInformation(
            "UploaderHandler [END] - TotalUploaded: {TotalUploaded}",
            uploaded);
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

    private async Task<long> ProcessBatchAsync(
        IReadOnlyList<FileEntity> files,
        Storage storage,
        DateTime windowEnd,
        CancellationToken cancellationToken)
    {
        long uploaded = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsWithinWindow(windowEnd))
                break;

            await UploadFileAsync(
                file,
                storage,
                cancellationToken);

            uploaded++;
        }

        return uploaded;
    }

    private async Task UploadFileAsync(
        FileEntity file,
        Storage storage,
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
