using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Availabilities;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public async Task CreateAvailabilityAsync_ExcludesHolidaysFromGeneratedSlots()
        {
            using var context = CreateInMemoryContext();

            // Sembrar Especialidad y Doctor
            var speciality = new Speciality("Cardiología", "Corazón");
            context.Set<Speciality>().Add(speciality);
            await context.SaveChangesAsync();

            var doctor = new Doctor("Dr. Pérez", speciality.Id);
            context.Set<Doctor>().Add(doctor);
            await context.SaveChangesAsync();

            var service = new AvailabilityService(context);

            // Crear disponibilidad para los lunes de Mayo de 2026 de 9:00 a 10:00
            // Nota: 2026-05-25 es Lunes y está cargado en holidays.json como feriado.
            var request = new CreateAvailabilityRequest
            {
                DoctorId = doctor.Id,
                Month = 5,
                Year = 2026,
                Days = new List<DaySlot>
                {
                    new DaySlot
                    {
                        DayOfWeek = 1, // Lunes
                        StartTime = TimeSpan.FromHours(9),
                        EndTime = TimeSpan.FromHours(10) // Debería generar 2 slots por Lunes (9:00-9:30, 9:30-10:00)
                    }
                }
            };

            var result = await service.CreateAvailabilityAsync(request);

            Assert.NotEmpty(result);

            // Recuperar todos los slots generados
            var slots = await context.Set<AvailabilitySlot>()
                .Where(s => s.AvailabilityRule.DoctorId == doctor.Id)
                .ToListAsync();

            // En Mayo de 2026 los lunes son: 4, 11, 18, 25.
            // May 25 es feriado, por lo que sólo se debieron generar slots para: 4, 11 y 18 (3 lunes * 2 slots = 6 slots)
            Assert.Equal(6, slots.Count);

            // Verificar que no hay ningún slot para el 25 de Mayo
            var hasHolidaySlots = slots.Any(s => s.SlotDate.Date == new DateTime(2026, 5, 25).Date);
            Assert.False(hasHolidaySlots);

            // Verificar que sí hay slots para el 18 de Mayo
            var hasWorkingMondaySlots = slots.Any(s => s.SlotDate.Date == new DateTime(2026, 5, 18).Date);
            Assert.True(hasWorkingMondaySlots);
        }
    }
}
