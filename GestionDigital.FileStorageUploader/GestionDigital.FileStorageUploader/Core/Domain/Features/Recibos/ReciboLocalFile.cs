namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;

public class ReciboLocalFile
{
    public int Id { get; set; }
    public required byte[] ContenidoOriginal { get; set; }
    public byte[]? ContenidoFirmadoEmpleador { get; set; }
    public byte[]? ContenidoFirmadoEmpleado { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime Timestamp { get; set; }

    public bool PendingDelete { get; set; }
    public Recibo Recibo { get; set; } = null!;
}
