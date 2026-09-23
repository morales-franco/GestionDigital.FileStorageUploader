using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class ReciboCloudFileConfiguration : IEntityTypeConfiguration<ReciboCloudFile>
{
    public void Configure(EntityTypeBuilder<ReciboCloudFile> builder)
    {

        builder.ToTable("ReciboCloudFiles");

        builder.HasKey(e => e.Id);

        builder.Property(x => x.Id)
               .HasColumnName("ReciboId");
    }
}
