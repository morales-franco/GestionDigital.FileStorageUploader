namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;

public class Storage
{
    public const string Local = "LOCAL";
    public const string Cloud = "CLOUD";

    public required string Code { get; set; }
    public string? BucketCloudId { get; set; }
    public bool DefaultStorage { get; set; }
    public required string StorageType { get; set; }
    public DateTime CreatedDate { get; set; }
}
