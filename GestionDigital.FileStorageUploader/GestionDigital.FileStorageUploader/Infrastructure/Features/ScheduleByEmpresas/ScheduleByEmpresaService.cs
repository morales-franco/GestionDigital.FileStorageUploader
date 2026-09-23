using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas;
using GestionDigital.FileStorageUploader.Core.Domain.Features.ScheduleByEmpresas.Services;
using GestionDigital.FileStorageUploader.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace GestionDigital.FileStorageUploader.Infrastructure.Features.ScheduleByEmpresas;

public class ScheduleByEmpresaService : IScheduleByEmpresaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;


    public ScheduleByEmpresaService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<ScheduleByEmpresa>> GetAsync()
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var now = DateOnly.FromDateTime(DateTime.Now);

            var schedules = await context.SchedulesByEmpresas
                                         .Where(s => s.StartSyncAt <= now
                                                     && s.Enabled
                                                     && !context.ScheduleByEmpresaExecutions
                                                                .Any(e => e.ScheduleByEmpresaEmpresaId == s.EmpresaId
                                                                          && e.Status == ScheduleByEmpresaExecutionStatus.RUNNING))
                                         .OrderBy(s => s.StartSyncAt)
                                         .ToListAsync();

            return schedules;
        }
    }
}
