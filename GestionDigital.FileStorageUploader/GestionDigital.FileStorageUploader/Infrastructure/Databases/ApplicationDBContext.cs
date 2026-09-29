using GestionDigital.FileStorageUploader.Core.Domain.Features.Storages;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Files;
using FileEntity = GestionDigital.FileStorageUploader.Core.Domain.Features.Files.File;
using GestionDigital.FileStorageUploader.Infrastructure.Databases.Configurations;
using Microsoft.EntityFrameworkCore;
using GestionDigital.FileStorageUploader.Core.Domain.Features.UploadExecutions;

namespace GestionDigital.FileStorageUploader.Infrastructure.Databases;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StorageConfiguration).Assembly);

    public DbSet<Storage> Storages { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<LocalFile> LocalFiles { get; set; }
    public DbSet<CloudFile> CloudFiles { get; set; }
    public DbSet<UploadExecution> UploadExecutions { get; set; }
}
