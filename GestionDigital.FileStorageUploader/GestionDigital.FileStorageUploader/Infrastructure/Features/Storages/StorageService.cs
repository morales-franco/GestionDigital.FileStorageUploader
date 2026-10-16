using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages.Services;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.Storages;

public class StorageService(IDbContextFactory<ApplicationDbContext> contextFactory) : IStorageService
{
    public async Task<Storage> GetByCodeAsync(string code)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Storages.AsNoTracking().SingleOrDefaultAsync(storage => storage.Code == code)
            ?? throw new InvalidOperationException($"Storage with code '{code}' not found.");
    }
}
