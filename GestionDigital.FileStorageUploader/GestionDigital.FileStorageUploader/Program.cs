using GestionDigital.FileStorageUploader.Core.Application.Features.DeleteLocalFiles;
using GestionDigital.FileStorageUploader.Core.Application.Features.UploadRecibos;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Recibos.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas.Services;
using GestionDigital.FileStorageUploader.Core.Domain.Features.Settings;
using GestionDigital.FileStorageUploader.Core.Domain.Features.StorageTypes.Services;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using GestionDigital.FileStorageUploader.Infrastructure.Features.Recibos;
using GestionDigital.FileStorageUploader.Infrastructure.Features.ScheduleByEmpresas;
using GestionDigital.FileStorageUploader.Infrastructure.Features.StorageTypes;
using GestionDigital.FileStorageUploader.Workers;
using Google.Apis.Core;
using Google.Apis.Http;
using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using Serilog;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "GestionDigital.FileStorageUploader";
        });

        builder.Services.AddHostedService<UploaderWorker>();
        builder.Services.AddMemoryCache();

        builder.Services.AddHostedService<DeleteLocalFileWorker>();

        builder.Services.AddDbContextFactory<ApplicationDbContext>(
                  (provider, options) =>
                  {
                      var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                      options
                      .UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"),
                          sqlOptions =>
                          {
                              sqlOptions.UseCompatibilityLevel(120);
                              sqlOptions.CommandTimeout(300);
                          }) // Timeout en segundos
                      .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                      .UseLoggerFactory(loggerFactory);
                  });

        builder.Services.AddSerilog((services, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(builder.Configuration));

        builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("StorageSettings"));
        builder.Services.Configure<DeleteLocalFileSettings>(builder.Configuration.GetSection("DeleteLocalFileSettings"));

        builder.Services.AddScoped<IUploaderHandler, UploaderHandler>();
        builder.Services.AddScoped<IDeleteLocalFileHandler, DeleteLocalFileHandler>();
        builder.Services.AddScoped<IReciboService, ReciboService>();
        builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();
        builder.Services.AddScoped<IScheduleByEmpresaService, ScheduleByEmpresaService>();
        builder.Services.AddScoped<IScheduleByEmpresaExecutionService, ScheduleByEmpresaExecutionService>();
        builder.Services.AddSingleton(_ =>
        {
            var timeoutSeconds = builder.Configuration.GetValue("CloudStorageSettings:RequestTimeoutInSeconds", 600);

            return new StorageClientBuilder
            {
                HttpClientFactory = new TimeoutHttpClientFactory(TimeSpan.FromSeconds(timeoutSeconds))
            }.Build();
        });

        builder.Services.AddKeyedScoped<IStorageTypeService,StorageTypeService>("inner");
        builder.Services.AddScoped<IStorageTypeService, StorageTypeServiceProxy>();

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", builder.Configuration.GetValue<string>("CloudStorageSettings:StorageKey"));

        var host = builder.Build();
        host.Run();
    }

    private sealed class TimeoutHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClientFactory _innerFactory = new();
        private readonly TimeSpan _timeout;

        public TimeoutHttpClientFactory(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            args.Initializers.Add(new HttpTimeoutInitializer(_timeout));

            return _innerFactory.CreateHttpClient(args);
        }
    }
}
