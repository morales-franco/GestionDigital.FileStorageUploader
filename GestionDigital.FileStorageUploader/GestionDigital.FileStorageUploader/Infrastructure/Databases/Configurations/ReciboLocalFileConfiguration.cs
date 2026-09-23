using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class ReciboLocalFileConfiguration : IEntityTypeConfiguration<ReciboLocalFile>
{
    public void Configure(EntityTypeBuilder<ReciboLocalFile> builder)
    {

        builder.ToTable("ReciboLocalFiles");

        builder.HasKey(e => e.Id);

        builder.Property(x => x.Id)
               .HasColumnName("ReciboId");
    }
}
