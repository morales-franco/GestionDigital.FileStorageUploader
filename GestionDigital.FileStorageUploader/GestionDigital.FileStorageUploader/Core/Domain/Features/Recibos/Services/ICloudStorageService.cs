using CSharpFunctionalExtensions;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos.Services;

public interface ICloudStorageService
{
    Task<Result<ReciboCloudIdentifier>> UploadAsync(Recibo recibo, CancellationToken cancellationToken);
}

public record ReciboCloudIdentifier(string? ContenidoOriginalId, string? ContenidoFirmadoEmpleadorId, string? ContenidoFirmadoEmpleadoId);
