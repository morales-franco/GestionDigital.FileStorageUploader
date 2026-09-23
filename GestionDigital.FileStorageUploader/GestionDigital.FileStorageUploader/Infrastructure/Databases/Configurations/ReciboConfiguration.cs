using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class ReciboConfiguration : IEntityTypeConfiguration<Recibo>
{
    public void Configure(EntityTypeBuilder<Recibo> builder) { 

        builder.ToTable("Recibos");

        builder.HasKey(e => e.Id);

        builder.Property(x => x.Id)
               .HasColumnName("ReciboId");

        builder.Property(x => x.StorageTypeId)
               .HasColumnName("StorageTypeId");

        builder
            .HasOne(x => x.LocalFile)
            .WithOne(x => x.Recibo)
            .HasForeignKey<ReciboLocalFile>(x => x.Id)
            .IsRequired(false);

        builder
            .HasOne(x => x.CloudFile)
            .WithOne()
            .HasForeignKey<ReciboCloudFile>(x => x.Id)
            .IsRequired(false);
    }
}
