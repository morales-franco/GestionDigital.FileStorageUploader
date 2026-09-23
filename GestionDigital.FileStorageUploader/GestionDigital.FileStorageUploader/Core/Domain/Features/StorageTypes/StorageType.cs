using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;

public class StorageType
{
    public const string LOCAL = "LOCAL";

    public string Code { get; set; }
    public string? BucketCloudId { get; set; }
    public IEnumerable<Recibo> Recibos { get; set; }
}
