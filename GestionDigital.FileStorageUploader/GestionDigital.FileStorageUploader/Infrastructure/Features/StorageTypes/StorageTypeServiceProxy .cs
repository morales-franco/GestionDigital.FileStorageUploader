using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;
using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.StorageTypes
{
    public class StorageTypeServiceProxy: IStorageTypeService
    {
        private readonly IStorageTypeService _innerService;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;

        public StorageTypeServiceProxy([FromKeyedServices("inner")]IStorageTypeService innerService, IMemoryCache cache, ILogger<StorageTypeServiceProxy> logger)
        {
            _innerService = innerService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<StorageType> GetByCodeAsync(string code) {

            const int expirationTime = 12;

            if (_cache.TryGetValue(code, out StorageType cachedStorageType))
            {
                return cachedStorageType;
            }

            _logger.LogInformation($"Cache miss for StorageType with code '{code}'. Fetching from inner service.");

            StorageType storageType = await _innerService.GetByCodeAsync(code);

            if (storageType != null) { 
                _cache.Set(code, storageType, TimeSpan.FromHours(expirationTime));
            }

            return storageType;
        }
    }
}

