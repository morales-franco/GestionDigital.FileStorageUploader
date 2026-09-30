namespace GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;

public class UploadExecution
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = UploadExecutionStatus.RUNNING;
    public required string TargetStorageTypeId { get; set; }
    public int ExpectedFilesCount { get; set; }
    public int UploadedFilesCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedDate { get; set; }
}

public static class UploadExecutionStatus
{
    public const string RUNNING = "RUNNING";
    public const string SUCCESS = "SUCCESS";
    public const string SUCCESS_INTERRUPTED = "SUCCESS_INTERRUPTED";
    public const string FAILED = "FAILED";
}
