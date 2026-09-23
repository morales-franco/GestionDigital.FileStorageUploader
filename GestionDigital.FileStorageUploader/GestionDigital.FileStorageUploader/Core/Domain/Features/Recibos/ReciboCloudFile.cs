namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;

public class ReciboCloudFile
{
    public int Id { get; set; }
    public required string ContenidoOriginalId { get; set; }
    public string? ContenidoFirmadoEmpleadorId { get; set; }
    public string? ContenidoFirmadoEmpleadoId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime Timestamp { get; set; }
}