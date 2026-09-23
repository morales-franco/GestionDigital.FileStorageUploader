using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;
using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes.Services;
using Microsoft.EntityFrameworkCore;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.StorageTypes
{
    public class StorageTypeService: IStorageTypeService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public StorageTypeService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<StorageType> GetByCodeAsync(string code)
        {
            using var context = _contextFactory.CreateDbContext();

            var storageType = await context.StorageTypes
                                           .FirstOrDefaultAsync(st => st.Code == code);

            if (storageType is null)
                throw new InvalidOperationException($"StorageType with code '{code}' not found.");

            return storageType;
        }
    }
}
