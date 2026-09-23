namespace GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;

public class ScheduleByEmpresaExecution
{
    public int Id { get; set; }
    public long ScheduleByEmpresaEmpresaId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = ScheduleByEmpresaExecutionStatus.RUNNING;
    public string TargetStorageTypeId { get; set; } = string.Empty;
    public int ExpectedRecibosCount { get; set; }
    public int UploadedRecibosCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedDate { get; set; }

    public ScheduleByEmpresa? ScheduleByEmpresa { get; set; }
}
