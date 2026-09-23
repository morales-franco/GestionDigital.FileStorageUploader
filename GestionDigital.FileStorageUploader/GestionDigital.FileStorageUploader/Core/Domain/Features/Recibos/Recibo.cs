using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;

public class Recibo
{
    public int Id { get; set; }
  
    public long RazonSocialId { get; set; }
    public long EmpresaId { get; set; }
    public DateTime Periodo { get; set; }
    public DateTime? FirmaEmpleadoDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime Timestamp { get; set; }

    public required string StorageTypeId { get; set; } 
    public StorageType StorageType { get; set; } = null!;

    public ReciboCloudFile? CloudFile { get; private set; }
    public ReciboLocalFile? LocalFile { get; private set; }

    public void SetCloudFile(ReciboCloudFile cloudFile)
    {
        this.CloudFile = cloudFile;
    }
}