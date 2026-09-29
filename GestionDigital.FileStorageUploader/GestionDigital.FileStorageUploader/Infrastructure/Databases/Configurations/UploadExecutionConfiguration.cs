using GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class UploadExecutionConfiguration : IEntityTypeConfiguration<UploadExecution>
{
    public void Configure(EntityTypeBuilder<UploadExecution> builder)
    {
        builder.ToTable("UploadExecutions", "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.StartedAt).HasColumnType("datetime");
        builder.Property(x => x.FinishedAt).HasColumnType("datetime");
        builder.Property(x => x.Status).HasMaxLength(20).IsUnicode(false)
            .HasDefaultValue(UploadExecutionStatus.RUNNING).IsRequired();
        builder.Property(x => x.TargetStorageTypeId).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.ExpectedFilesCount).HasDefaultValue(0L);
        builder.Property(x => x.UploadedFilesCount).HasDefaultValue(0L);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.HasIndex(x => x.StartedAt);
    }
}
