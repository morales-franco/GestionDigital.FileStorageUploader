
namespace GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas
{
    public  class ScheduleByEmpresa
    {
        public long EmpresaId { get; set; }
        public short MonthsInLocalDB { get; set; }
        public DateOnly StartSyncAt { get; set; }
        public bool Enabled { get; set; }

        public DateTime CreatedDate { get; set; }
        public ICollection<ScheduleByEmpresaExecution> Executions { get; set; } = new List<ScheduleByEmpresaExecution>();
    }
}
