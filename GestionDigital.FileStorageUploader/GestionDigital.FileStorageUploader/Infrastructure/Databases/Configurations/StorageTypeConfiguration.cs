using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class StorageTypeConfiguration : IEntityTypeConfiguration<StorageType>
{
    public void Configure(EntityTypeBuilder<StorageType> builder)
    {
        builder.ToTable("StorageTypes");

        builder.HasKey(e => e.Code);

        builder.HasMany(x => x.Recibos)
               .WithOne(x => x.StorageType)
               .HasForeignKey(x => x.StorageTypeId)
               .IsRequired(true);
    }
}
