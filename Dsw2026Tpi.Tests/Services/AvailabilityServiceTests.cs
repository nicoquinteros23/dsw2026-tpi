using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Availabilities;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dsw2026Tpi.Tests.Services
{
    public class AvailabilityServiceTests
    {
        private static TestDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<Dsw2026Tpi.Data.Dsw2026TpiDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new TestDbContext(options);
        }

        // La consigna exige que la disponibilidad se cree SIEMPRE para "el resto del
        // mes en curso" (el año/mes ya no se puede mandar por request), así que este
        // test no puede fijar una fecha arbitraria como antes (mayo 2026): calcula
        // sobre el mes actual usando DateTime.Now, igual que hace el servicio.
        [Fact]
        public async Task CreateAvailabilityAsync_ExcludesHolidaysFromGeneratedSlots()
        {
            using var context = CreateInMemoryContext();

            // Sembrar Especialidad y Doctor
            var speciality = new Speciality("Cardiología", "Corazón");
            context.Set<Speciality>().Add(speciality);
            await context.SaveChangesAsync();

            var doctor = new Doctor("Dr. Pérez", "MP-11111", speciality.Id);
            context.Set<Doctor>().Add(doctor);
            await context.SaveChangesAsync();

            var service = new AvailabilityService(context);

            var today = DateTime.Now.Date;
            var targetDayOfWeek = SpanishDayOfWeek.ToSpanishName((byte)today.DayOfWeek);

            var request = new CreateAvailabilityRequest
            {
                DoctorId = doctor.Id,
                Days = new List<AvailabilityDayRequest>
                {
                    new AvailabilityDayRequest
                    {
                        Day = targetDayOfWeek,
                        StartTime = "09:00",
                        EndTime = "10:00" // Debería generar 2 slots de 30 min por día
                    }
                }
            };

            var result = await service.CreateAvailabilityAsync(request);

            Assert.NotEmpty(result);

            // Recuperar todos los slots generados
            var slots = await context.Set<AvailabilitySlot>()
                .Where(s => s.AvailabilityRule.DoctorId == doctor.Id)
                .ToListAsync();

            var holidays = LoadHolidays();
            var lastDayOfMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);

            // Fechas donde debería haberse generado disponibilidad: desde HOY (no desde
            // el día 1) hasta fin de mes, para el día de la semana elegido, salvo feriados.
            var expectedDates = new List<DateTime>();
            for (var date = today; date <= lastDayOfMonth; date = date.AddDays(1))
            {
                if (date.DayOfWeek == today.DayOfWeek && !holidays.Contains(date.Date))
                    expectedDates.Add(date.Date);
            }

            Assert.Equal(expectedDates.Count * 2, slots.Count);

            foreach (var expectedDate in expectedDates)
                Assert.Contains(slots, s => s.SlotDate.Date == expectedDate);

            // Ningún slot generado debería caer en un feriado del mes en curso
            foreach (var holidayInMonth in holidays.Where(h => h.Year == today.Year && h.Month == today.Month))
                Assert.DoesNotContain(slots, s => s.SlotDate.Date == holidayInMonth);

            // Ningún slot generado debería caer antes de hoy (la disponibilidad es
            // siempre para "el resto del mes en curso")
            Assert.DoesNotContain(slots, s => s.SlotDate.Date < today);
        }

        private static HashSet<DateTime> LoadHolidays()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sources", "holidays.json");
            if (!File.Exists(path))
                return new HashSet<DateTime>();

            var json = File.ReadAllText(path);
            var stringDates = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return stringDates.Select(d => DateTime.Parse(d).Date).ToHashSet();
        }
    }
}
