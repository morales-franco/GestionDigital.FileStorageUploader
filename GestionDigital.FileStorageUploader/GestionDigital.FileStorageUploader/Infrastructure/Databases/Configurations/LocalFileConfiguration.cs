using GestionDigital.FileStorageUploader.Core.Domain.Features.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class LocalFileConfiguration : IEntityTypeConfiguration<LocalFile>
{
    public void Configure(EntityTypeBuilder<LocalFile> builder)
    {
        builder.ToTable("LocalFiles", "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Content).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(x => x.PendingDelete).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime");
        builder.Property(x => x.Timestamp).HasColumnType("datetime");
    }
}
