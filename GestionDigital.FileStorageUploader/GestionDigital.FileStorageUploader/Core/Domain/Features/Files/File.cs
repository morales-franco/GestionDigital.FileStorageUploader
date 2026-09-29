using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Files;

public class File
{
    public long Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public string StorageId { get; set; } = Storage.Local;
    public Storage Storage { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime Timestamp { get; set; }
    public LocalFile? LocalFile { get; set; }
    public CloudFile? CloudFile { get; set; }
}
