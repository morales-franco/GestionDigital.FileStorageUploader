namespace GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas.Services;

public interface IScheduleByEmpresaService
{
    Task<IEnumerable<ScheduleByEmpresa>> GetAsync();
}
