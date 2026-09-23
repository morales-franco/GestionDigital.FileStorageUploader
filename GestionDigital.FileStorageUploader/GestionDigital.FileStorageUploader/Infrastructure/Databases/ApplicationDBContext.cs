using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos;
using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;
using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes;
using GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;
using Microsoft.EntityFrameworkCore;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScheduleByEmpresaConfiguration).Assembly);
    public DbSet<ScheduleByEmpresa> SchedulesByEmpresas { get; set; }
    public DbSet<ScheduleByEmpresaExecution> ScheduleByEmpresaExecutions { get; set; }
    public DbSet<Recibo> Recibos { get; set; }
    public DbSet<ReciboCloudFile> ReciboCloudFiles { get; set; }
    public DbSet<ReciboLocalFile> ReciboLocalFiles { get; set; }

    public DbSet<StorageType> StorageTypes { get; set; }
}
