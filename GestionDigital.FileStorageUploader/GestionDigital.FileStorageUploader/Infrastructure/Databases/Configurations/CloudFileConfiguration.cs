using GestionDigital.FileStorageUploader.Core.Domain.Features.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class CloudFileConfiguration : IEntityTypeConfiguration<CloudFile>
{
    public void Configure(EntityTypeBuilder<CloudFile> builder)
    {
        builder.ToTable("CloudFiles", "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FileCloudId).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.Property(x => x.Timestamp).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
    }
}
