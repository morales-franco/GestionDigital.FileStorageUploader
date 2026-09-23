using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes.Services
{
    public interface IStorageTypeService
    {
        Task<StorageType> GetByCodeAsync(string code);
    }
}
