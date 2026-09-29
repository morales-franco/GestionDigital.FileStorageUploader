namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Files;

public class CloudFile
{
    public long Id { get; set; }
    public required string FileCloudId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime Timestamp { get; set; }
}
