namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;

public class StorageSettings
{
    public string DefaultStorageTypeId { get; set; } = string.Empty;
    public int MonthsInDb { get; set; } = 12;
}
