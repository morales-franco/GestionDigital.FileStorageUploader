using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class ScheduleByEmpresaExecutionConfiguration : IEntityTypeConfiguration<ScheduleByEmpresaExecution>
{
    public void Configure(EntityTypeBuilder<ScheduleByEmpresaExecution> builder)
    {
        builder.ToTable("ScheduleByEmpresaExecutions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.Status)
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(x => x.TargetStorageTypeId)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.ErrorMessage)
               .HasMaxLength(1000);

        builder.HasIndex(x => x.ScheduleByEmpresaEmpresaId);
        builder.HasIndex(x => x.ScheduleByEmpresaEmpresaId)
               .HasDatabaseName("UX_ScheduleByEmpresaExecutions_OneRunningByEmpresa")
               .HasFilter("[Status] = 'RUNNING'")
               .IsUnique();

        builder.HasIndex(x => x.StartedAt);

        builder.HasOne(x => x.ScheduleByEmpresa)
               .WithMany(x => x.Executions)
               .HasForeignKey(x => x.ScheduleByEmpresaEmpresaId)
               .HasPrincipalKey(x => x.EmpresaId);
    }
}
