
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;

public class ScheduleByEmpresaConfiguration: IEntityTypeConfiguration<ScheduleByEmpresa>
{
    public void Configure(EntityTypeBuilder<ScheduleByEmpresa> builder) { 

        builder.ToTable("SchedulesByEmpresa");

        builder.HasKey(e => e.EmpresaId);
    }
}
