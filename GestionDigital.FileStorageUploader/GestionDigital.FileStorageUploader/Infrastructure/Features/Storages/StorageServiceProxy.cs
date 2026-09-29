using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages.Services;
using Microsoft.Extensions.Caching.Memory;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Storages;

public class StorageServiceProxy(
    [FromKeyedServices("inner")] IStorageService innerService,
    IMemoryCache cache,
    ILogger<StorageServiceProxy> logger) : IStorageService
{
    public async Task<Storage> GetByCodeAsync(string code)
    {
        var cacheKey = (typeof(Storage), code);
        if (cache.TryGetValue(cacheKey, out Storage? storage) && storage is not null)
            return storage;

        logger.LogInformation("Cache miss for Storage with code '{Code}'. Fetching from inner service.", code);
        storage = await innerService.GetByCodeAsync(code);
        cache.Set(cacheKey, storage, TimeSpan.FromHours(12));
        return storage;
    }
}
