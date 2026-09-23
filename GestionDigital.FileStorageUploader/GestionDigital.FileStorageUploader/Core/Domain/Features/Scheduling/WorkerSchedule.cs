using System.Globalization;

namespace GestionDigital.FileStorageUploader.Core.Domain.Features.Scheduling;

public sealed class WorkerScheduleSettings
{
    public bool Enable { get; set; } = true;
    public List<WeeklyWindowSettings> Schudulers { get; set; } = new();
}

public sealed class WeeklyWindowSettings
{
    public string StartDay { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndDay { get; set; } = "";
    public string EndTime { get; set; } = "";
}

public sealed record WorkWindow(DateTime Start, DateTime End);

public sealed class WorkerSchedule
{
    private readonly List<(TimeSpan Start, TimeSpan Duration)> _windows = new();
    public bool Enabled { get; }

    public WorkerSchedule(WorkerScheduleSettings settings)
    {
        Enabled = settings.Enable;
        foreach (var window in settings.Schudulers)
        {
            var start = Parse(window.StartDay, window.StartTime);
            var end = Parse(window.EndDay, window.EndTime);
            var duration = end - start;
            if (duration == TimeSpan.Zero)
                throw new ArgumentException("A schedule must have different start and end times.");
            // Los días se cuentan desde el domingo: sábado 09:00 -> lunes 00:00 da una resta negativa.
            // Sumamos 7 días porque el lunes pertenece a la semana siguiente; la duración queda en 39 horas.
            if (duration < TimeSpan.Zero) duration += TimeSpan.FromDays(7);
            _windows.Add((start, duration));
        }
        if (_windows.Count == 0)
            throw new ArgumentException("schudulers must contain at least one weekly schedule.");
        for (var i = 0; i < _windows.Count; i++)
            for (var j = i + 1; j < _windows.Count; j++)
                for (var offset = -7; offset <= 7; offset += 7)
                {
                    var a = _windows[i];
                    var b = _windows[j];
                    var bs = b.Start + TimeSpan.FromDays(offset);
                    if (a.Start < bs + b.Duration && bs < a.Start + a.Duration)
                        throw new ArgumentException("schudulers contains overlapping schedules.");
                }
    }

    private static TimeSpan Parse(string day, string time)
    {
        if (!Enum.TryParse<DayOfWeek>(day, true, out var parsedDay) ||
            !Enum.IsDefined(parsedDay) ||
            !TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
            throw new ArgumentException($"Invalid schedule day/time: {day} {time}. Use a day name and HH:mm.");
        return TimeSpan.FromDays((int)parsedDay) + parsedTime.ToTimeSpan();
    }

    public WorkWindow Next(DateTime now, DateTime? lastExecutedWindowStart = null)
    {
        var currentWeekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        WorkWindow? nextWindow = null;

        // Incluimos la semana anterior por las ventanas que cruzan de sábado a lunes.
        foreach (var weekOffset in new[] { -1, 0, 1 })
        {
            var weekStart = currentWeekStart.AddDays(weekOffset * 7);

            foreach (var schedule in _windows)
            {
                var start = weekStart.Add(schedule.Start);
                var end = start.Add(schedule.Duration);

                if (end <= now)
                    continue;

                if (lastExecutedWindowStart.HasValue && start <= lastExecutedWindowStart.Value)
                    continue;

                if (nextWindow is null || start < nextWindow.Start)
                    nextWindow = new WorkWindow(start, end);
            }
        }

        return nextWindow
            ?? throw new InvalidOperationException(
                "No se encontró una ventana de ejecución disponible.");
    }
}
