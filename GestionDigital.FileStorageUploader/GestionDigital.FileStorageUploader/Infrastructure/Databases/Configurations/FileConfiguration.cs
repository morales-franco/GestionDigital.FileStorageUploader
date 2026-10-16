using GestionDigital.FileStorageUploader.Core.Domain.Features.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("Files", "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.FileName).HasMaxLength(255).IsUnicode(false).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.StorageId).HasMaxLength(50).IsUnicode(false).HasDefaultValue("LOCAL").IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.Property(x => x.Timestamp).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.HasOne(x => x.Storage).WithMany().HasForeignKey(x => x.StorageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.LocalFile).WithOne().HasForeignKey<LocalFile>(x => x.Id).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.CloudFile).WithOne().HasForeignKey<CloudFile>(x => x.Id).OnDelete(DeleteBehavior.NoAction);
    }
}
