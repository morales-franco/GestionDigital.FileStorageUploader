using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos.Services;

public interface IReciboService
{
    Task<int> CountToUploadAsync(ScheduleByEmpresa scheduleByEmpresa, CancellationToken cancellationToken);
    Task<int> DeletePendingRecibosAsync(DateTime windowEnd, CancellationToken cancellationToken);
    Task<IEnumerable<Recibo>> GetRecibosToUploadAsync(ScheduleByEmpresa scheduleByEmpresa, int batchSize, CancellationToken cancellationToken);
    Task MarkRecibosAsUploaded(IEnumerable<Recibo> recibos, CancellationToken cancellationToken);
}
