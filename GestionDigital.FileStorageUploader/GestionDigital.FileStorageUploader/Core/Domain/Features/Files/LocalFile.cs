namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Files;

public class LocalFile
{
    public long Id { get; set; }
    public required byte[] Content { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime Timestamp { get; set; }
    public bool PendingDelete { get; set; }
}
