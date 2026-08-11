using Dsw2026Tpi.Application.Dtos.Availabilities;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly Dsw2026TpiDbContext _context;
    private const int SLOT_DURATION_MINUTES = 30;

    public AvailabilityService(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    private record ParsedDay(byte DayOfWeek, TimeSpan StartTime, TimeSpan EndTime);

    public async Task<List<AvailabilityRuleResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request)
    {
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && !d.IsDeleted);

        if (doctor == null)
            throw new EntityNotFoundException("Doctor");

        var today = DateTime.Now;
        var year = (short)today.Year;
        var month = (byte)today.Month;

        var parsedDays = ParseDays(request.Days);
        ValidateNoOverlaps(parsedDays);

        // Eliminar reglas existentes para el mes en curso
        var existingRules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == request.DoctorId && ar.Year == year && ar.Month == month)
            .ToListAsync();

        if (existingRules.Any())
            _context.Set<AvailabilityRule>().RemoveRange(existingRules);

        // Crear nuevas reglas de disponibilidad
        var rules = new List<AvailabilityRule>();
        foreach (var day in parsedDays)
        {
            var rule = new AvailabilityRule(
                request.DoctorId,
                month,
                year,
                day.DayOfWeek,
                day.StartTime,
                day.EndTime
            );
            rules.Add(rule);
            _context.Set<AvailabilityRule>().Add(rule);
        }

        await _context.SaveChangesAsync();

        // Generar slots de 30 minutos para el resto del mes en curso
        await GenerateSlotsForMonth(request.DoctorId, year, month);

        return MapRules(rules);
    }

    public async Task<List<AvailabilityRuleResponse>> UpdateAvailabilityAsync(CreateAvailabilityRequest request)
    {
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && !d.IsDeleted);

        if (doctor == null)
            throw new EntityNotFoundException("Doctor");

        var today = DateTime.Now;
        var year = (short)today.Year;
        var month = (byte)today.Month;

        var parsedDays = ParseDays(request.Days);
        ValidateNoOverlaps(parsedDays);

        // Obtener reglas existentes para el mes en curso
        var existingRules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == request.DoctorId && ar.Year == year && ar.Month == month)
            .ToListAsync();

        if (existingRules.Any())
            _context.Set<AvailabilityRule>().RemoveRange(existingRules);

        // Eliminar slots no reservados del mes en curso
        var unBookedSlots = await _context.Set<AvailabilitySlot>()
            .Where(s => s.SlotDate >= new DateTime(year, month, 1) &&
                        s.SlotDate < new DateTime(year, month, 1).AddMonths(1) &&
                        s.Status != "BOOKED" &&
                        s.AvailabilityRule.DoctorId == request.DoctorId)
            .ToListAsync();

        _context.Set<AvailabilitySlot>().RemoveRange(unBookedSlots);

        // Crear nuevas reglas
        var rules = new List<AvailabilityRule>();
        foreach (var day in parsedDays)
        {
            var rule = new AvailabilityRule(
                request.DoctorId,
                month,
                year,
                day.DayOfWeek,
                day.StartTime,
                day.EndTime
            );
            rules.Add(rule);
            _context.Set<AvailabilityRule>().Add(rule);
        }

        await _context.SaveChangesAsync();

        await GenerateSlotsForMonth(request.DoctorId, year, month);

        return MapRules(rules);
    }

    public async Task<List<DoctorAvailabilityDayResponse>> GetDoctorAvailabilityAsync(Guid doctorId)
    {
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

        if (doctor == null)
            throw new EntityNotFoundException("Doctor");

        var today = DateTime.Now;
        var year = (short)today.Year;
        var month = (byte)today.Month;

        var rules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == doctorId && ar.Year == year && ar.Month == month)
            .ToListAsync();

        return rules
            .Select(r => new DoctorAvailabilityDayResponse
            {
                Day = SpanishDayOfWeek.ToSpanishName(r.DayOfWeek),
                StartTime = r.StartTime.ToString(@"hh\:mm"),
                EndTime = r.EndTime.ToString(@"hh\:mm")
            })
            .ToList();
    }

    private static List<ParsedDay> ParseDays(List<AvailabilityDayRequest> days)
    {
        return days.Select(d => new ParsedDay(
            SpanishDayOfWeek.ToDayOfWeekNumber(d.Day),
            ParseTime(d.StartTime),
            ParseTime(d.EndTime)
        )).ToList();
    }

    private static TimeSpan ParseTime(string value)
    {
        try
        {
            return TimeSpan.ParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new ValidationException($"Formato de hora inválido: '{value}'. Se espera HH:mm.", "INVALID_TIME_FORMAT");
        }
    }

    private static List<AvailabilityRuleResponse> MapRules(List<AvailabilityRule> rules)
    {
        return rules.Select(r => new AvailabilityRuleResponse
        {
            Day = SpanishDayOfWeek.ToSpanishName(r.DayOfWeek),
            StartTime = r.StartTime.ToString(@"hh\:mm"),
            EndTime = r.EndTime.ToString(@"hh\:mm")
        }).ToList();
    }

    private void ValidateNoOverlaps(List<ParsedDay> days)
    {
        var groupedByDay = days.GroupBy(d => d.DayOfWeek);

        foreach (var dayGroup in groupedByDay)
        {
            if (dayGroup.Count() > 1)
            {
                var daySlots = dayGroup.OrderBy(d => d.StartTime).ToList();
                for (int i = 0; i < daySlots.Count - 1; i++)
                {
                    if (daySlots[i].EndTime > daySlots[i + 1].StartTime)
                        throw new BusinessRuleException($"Existen solapamientos de horarios para el día {SpanishDayOfWeek.ToSpanishName(dayGroup.Key)}.", "AVAILABILITY_OVERLAP");
                }
            }
        }
    }

    private List<DateTime> GetHolidays()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sources", "holidays.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var stringDates = JsonSerializer.Deserialize<List<string>>(json);
                if (stringDates != null)
                {
                    return stringDates.Select(d => DateTime.Parse(d).Date).ToList();
                }
            }
        }
        catch
        {
            // Fallback
        }
        return new List<DateTime>();
    }

    private async Task GenerateSlotsForMonth(Guid doctorId, int year, int month)
    {
        var today = DateTime.Now.Date;
        var firstDayOfMonth = new DateTime(year, month, 1);
        var lastDay = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // La disponibilidad se genera siempre para el resto del mes en curso:
        // si hoy es 15, no se generan slots para los días 1 al 14.
        var startDate = (year == today.Year && month == today.Month && today > firstDayOfMonth)
            ? today
            : firstDayOfMonth;

        var rules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == doctorId && ar.Year == year && ar.Month == month)
            .ToListAsync();

        var holidays = GetHolidays();
        var slots = new List<AvailabilitySlot>();

        for (var date = startDate; date <= lastDay; date = date.AddDays(1))
        {
            if (holidays.Contains(date.Date))
            {
                continue;
            }

            var dayOfWeek = (byte)date.DayOfWeek;
            var applicableRule = rules.FirstOrDefault(r => r.DayOfWeek == dayOfWeek);

            if (applicableRule != null)
            {
                var currentTime = applicableRule.StartTime;
                while (currentTime < applicableRule.EndTime)
                {
                    var endTime = currentTime.Add(TimeSpan.FromMinutes(SLOT_DURATION_MINUTES));
                    if (endTime <= applicableRule.EndTime)
                    {
                        slots.Add(new AvailabilitySlot(applicableRule.Id, date, currentTime, endTime));
                    }
                    currentTime = endTime;
                }
            }
        }

        _context.Set<AvailabilitySlot>().AddRange(slots);
        await _context.SaveChangesAsync();
    }
}
