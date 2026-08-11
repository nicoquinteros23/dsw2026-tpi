using Dsw2026Tpi.Application.Dtos.Availabilities;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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

    public async Task<List<AvailabilityRuleResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request)
    {
        // Validar que el doctor exista
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && !d.IsDeleted);

        if (doctor == null)
            throw new EntityNotFoundException("Doctor");

        // Validar que no haya solapamientos entre los días enviados
        ValidateNoOverlaps(request.Days);

        // Eliminar reglas existentes para el mes/año especificado
        var existingRules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == request.DoctorId && ar.Year == request.Year && ar.Month == request.Month)
            .ToListAsync();

        if (existingRules.Any())
            _context.Set<AvailabilityRule>().RemoveRange(existingRules);

        // Crear nuevas reglas de disponibilidad
        var rules = new List<AvailabilityRule>();
        foreach (var day in request.Days)
        {
            var rule = new AvailabilityRule(
                request.DoctorId,
                request.Month,
                request.Year,
                day.DayOfWeek,
                day.StartTime,
                day.EndTime
            );
            rules.Add(rule);
            _context.Set<AvailabilityRule>().Add(rule);
        }

        await _context.SaveChangesAsync();

        // Generar slots de 30 minutos para el mes
        await GenerateSlotsForMonth(request.DoctorId, request.Year, request.Month, request.Days);

        return rules.Select(r => new AvailabilityRuleResponse
        {
            Id = r.Id,
            DoctorId = r.DoctorId,
            Month = r.Month,
            Year = r.Year,
            DayOfWeek = r.DayOfWeek,
            StartTime = r.StartTime,
            EndTime = r.EndTime
        }).ToList();
    }

    public async Task<List<AvailabilityRuleResponse>> UpdateAvailabilityAsync(CreateAvailabilityRequest request)
    {
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && !d.IsDeleted);

        if (doctor == null)
            throw new EntityNotFoundException("Doctor");

        ValidateNoOverlaps(request.Days);

        // Obtener reglas existentes para el período
        var existingRules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == request.DoctorId && ar.Year == request.Year && ar.Month == request.Month)
            .ToListAsync();

        if (existingRules.Any())
            _context.Set<AvailabilityRule>().RemoveRange(existingRules);

        // Eliminar slots no reservados del mes
        var unBookedSlots = await _context.Set<AvailabilitySlot>()
            .Where(s => s.SlotDate >= new DateTime(request.Year, request.Month, 1) &&
                        s.SlotDate < new DateTime(request.Year, request.Month, 1).AddMonths(1) &&
                        s.Status != "BOOKED" &&
                        s.AvailabilityRule.DoctorId == request.DoctorId)
            .ToListAsync();

        _context.Set<AvailabilitySlot>().RemoveRange(unBookedSlots);

        // Crear nuevas reglas
        var rules = new List<AvailabilityRule>();
        foreach (var day in request.Days)
        {
            var rule = new AvailabilityRule(
                request.DoctorId,
                request.Month,
                request.Year,
                day.DayOfWeek,
                day.StartTime,
                day.EndTime
            );
            rules.Add(rule);
            _context.Set<AvailabilityRule>().Add(rule);
        }

        await _context.SaveChangesAsync();

        await GenerateSlotsForMonth(request.DoctorId, request.Year, request.Month, request.Days);

        return rules.Select(r => new AvailabilityRuleResponse
        {
            Id = r.Id,
            DoctorId = r.DoctorId,
            Month = r.Month,
            Year = r.Year,
            DayOfWeek = r.DayOfWeek,
            StartTime = r.StartTime,
            EndTime = r.EndTime
        }).ToList();
    }

    public async Task<DoctorAvailabilityResponse> GetDoctorAvailabilityAsync(Guid doctorId, int month, int year)
    {
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

        if (doctor == null)
            throw new EntityNotFoundException("Doctor");

        var slots = await _context.Set<AvailabilitySlot>()
            .Where(s => s.SlotDate >= new DateTime(year, month, 1) &&
                        s.SlotDate < new DateTime(year, month, 1).AddMonths(1) &&
                        s.Status == "AVAILABLE" &&
                        s.AvailabilityRule.DoctorId == doctorId)
            .OrderBy(s => s.SlotDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return new DoctorAvailabilityResponse
        {
            DoctorId = doctorId,
            DoctorName = doctor.Name,
            Month = month,
            Year = year,
            AvailableSlots = slots.Select(s => new AvailabilitySlotResponse
            {
                Id = s.Id,
                SlotDate = s.SlotDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status
            }).ToList()
        };
    }

    private void ValidateNoOverlaps(List<DaySlot> days)
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
                        throw new BusinessRuleException($"Existen solapamientos de horarios para el día {dayGroup.Key}.", "AVAILABILITY_OVERLAP");
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

    private async Task GenerateSlotsForMonth(Guid doctorId, int year, int month, List<DaySlot> days)
    {
        var firstDay = new DateTime(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var rules = await _context.Set<AvailabilityRule>()
            .Where(ar => ar.DoctorId == doctorId && ar.Year == year && ar.Month == month)
            .ToListAsync();

        var holidays = GetHolidays();
        var slots = new List<AvailabilitySlot>();

        for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
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
