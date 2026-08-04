using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Availabilities;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Tests.Services;

/// <summary>
/// Pruebas unitarias para AvailabilityService
/// </summary>
public class AvailabilityServiceTests
{
    /// <summary>
    /// Crea un DbContext en memoria para los tests
    /// </summary>
    private static Dsw2026TpiDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Dsw2026TpiDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Dsw2026TpiDbContext(options);
    }

    /// <summary>
    /// PRUEBA 2: Verificar que se lanza excepción cuando hay solapamiento de horarios
    /// </summary>
    [Fact]
    public async Task CreateAvailabilityAsync_WithOverlappingTimeRanges_ThrowsInvalidOperationException()
    {
        // ARRANGE: Preparar base de datos en memoria
        using var context = CreateInMemoryContext(nameof(CreateAvailabilityAsync_WithOverlappingTimeRanges_ThrowsInvalidOperationException));

        var doctorId = Guid.NewGuid();

        // Crear especialidad y doctor usando los constructores reales
        var speciality = new Speciality("Cardiología", "Especialidad del corazón");
        context.Specialities.Add(speciality);
        await context.SaveChangesAsync();

        var doctor = new Doctor("Dr. Test", speciality.Id, doctorId);
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var availabilityService = new AvailabilityService(context);

        // Crear horarios con solapamiento: dos reglas para el mismo día con horarios que se cruzan
        var request = new CreateAvailabilityRequest
        {
            DoctorId = doctorId,
            Year = 2026,
            Month = 8,
            Days = new List<DaySlot>
            {
                // Lunes: 09:00-12:00
                new DaySlot
                {
                    DayOfWeek = 1,
                    StartTime = new TimeSpan(09, 00, 00),
                    EndTime = new TimeSpan(12, 00, 00)
                },
                // Lunes: 10:00-13:00 - ESTO SE SOLAPA CON EL ANTERIOR
                new DaySlot
                {
                    DayOfWeek = 1,
                    StartTime = new TimeSpan(10, 00, 00),
                    EndTime = new TimeSpan(13, 00, 00)
                }
            }
        };

        // ACT & ASSERT: Verificar que se lanza una excepción
        var exception = await Assert.ThrowsAsync<Exception>(
            () => availabilityService.CreateAvailabilityAsync(request)
        );

        // Verificar el mensaje de la excepción
        Assert.NotNull(exception);
        Assert.Contains("solapamiento", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
