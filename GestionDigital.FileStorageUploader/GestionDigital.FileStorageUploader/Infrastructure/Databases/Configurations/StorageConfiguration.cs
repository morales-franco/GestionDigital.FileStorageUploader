using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class StorageConfiguration : IEntityTypeConfiguration<Storage>
{
    public void Configure(EntityTypeBuilder<Storage> builder)
    {
        builder.ToTable("Storages", "dbo");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.BucketCloudId).HasMaxLength(50).IsUnicode(false).IsRequired(false);
        builder.Property(x => x.DefaultStorage).IsRequired();
        builder.Property(x => x.StorageType).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.HasIndex(x => x.StorageType).IsUnique().HasFilter("[DefaultStorage] = 1")
            .HasDatabaseName("UX_Storages_DefaultStorage_StorageType");
    }
}
