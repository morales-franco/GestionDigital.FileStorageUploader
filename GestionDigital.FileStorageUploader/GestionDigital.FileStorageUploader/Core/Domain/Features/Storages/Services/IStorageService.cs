namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Storages.Services;

public interface IStorageService
{
    Task<Storage> GetByCodeAsync(string code);
}
