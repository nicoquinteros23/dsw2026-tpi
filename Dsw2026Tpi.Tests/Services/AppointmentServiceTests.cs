using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Tests.Services;

/// <summary>
/// Pruebas unitarias para AppointmentService
/// </summary>
public class AppointmentServiceTests
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
    /// PRUEBA 1: Verificar que se lanza excepción cuando AvailabilitySlotId no existe
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithInvalidAvailabilitySlotId_ThrowsKeyNotFoundException()
    {
        // ARRANGE: Preparar base de datos en memoria
        using var context = CreateInMemoryContext(nameof(CreateAsync_WithInvalidAvailabilitySlotId_ThrowsKeyNotFoundException));

        // Crear un paciente en la base de datos en memoria
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Email = "paciente@test.com",
            Dni = "12345678",
            FullName = "Paciente Test"
        };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var appointmentService = new AppointmentService(context);

        // Request con un AvailabilitySlotId que NO existe en la BD
        var request = new AppointmentRequest
        {
            AvailabilitySlotId = Guid.NewGuid(), // ID inexistente
            Reason = "Consulta médica general"
        };

        // ACT & ASSERT: Verificar que se lanza una excepción
        var exception = await Assert.ThrowsAsync<Exception>(
            () => appointmentService.CreateAsync(request, "paciente@test.com")
        );

        // Verificar el mensaje de la excepción
        Assert.NotNull(exception);
        Assert.Contains("El horario disponible no existe", exception.Message);
    }

    /// <summary>
    /// PRUEBA 4: Happy Path - Crear un turno exitosamente cuando el slot existe y está disponible
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithValidSlotAvailable_CreatesAppointmentSuccessfully()
    {
        // ARRANGE: Preparar base de datos en memoria
        using var context = CreateInMemoryContext(nameof(CreateAsync_WithValidSlotAvailable_CreatesAppointmentSuccessfully));

        // Datos de prueba
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        // Crear paciente (Patient está en Dsw2026Tpi.Domain, sin constructor explícito)
        var patient = new Patient
        {
            Id = patientId,
            Email = "paciente@test.com",
            Dni = "12345678",
            FullName = "Paciente Test"
        };
        context.Patients.Add(patient);

        // Crear especialidad con el constructor real: (string name, string description, Guid? id = null)
        var speciality = new Speciality("Clínica Médica", "Medicina general");
        context.Specialities.Add(speciality);

        // Crear doctor con el constructor real: (string name, Guid specialityId, Guid? id = null)
        var doctor = new Doctor("Dr. Test", speciality.Id, doctorId);
        context.Doctors.Add(doctor);

        // Crear regla de disponibilidad con el constructor real:
        // (Guid doctorId, byte month, short year, byte dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? id = null)
        var availabilityRule = new AvailabilityRule(
            doctorId,
            month: 8,
            year: 2026,
            dayOfWeek: 1,
            startTime: new TimeSpan(09, 00, 00),
            endTime: new TimeSpan(17, 00, 00),
            id: ruleId
        );
        context.AvailabilityRules.Add(availabilityRule);

        // Crear slot de disponibilidad con el constructor real:
        // (Guid availabilityRuleId, DateTime slotDate, TimeSpan startTime, TimeSpan endTime, Guid? id = null)
        var availabilitySlot = new AvailabilitySlot(
            availabilityRuleId: ruleId,
            slotDate: new DateTime(2026, 8, 4),
            startTime: new TimeSpan(09, 00, 00),
            endTime: new TimeSpan(09, 30, 00),
            id: slotId
        );
        context.AvailabilitySlots.Add(availabilitySlot);

        await context.SaveChangesAsync();

        var appointmentService = new AppointmentService(context);

        var request = new AppointmentRequest
        {
            AvailabilitySlotId = slotId,
            Reason = "Consulta médica general"
        };

        // ACT: Crear el turno
        var response = await appointmentService.CreateAsync(request, "paciente@test.com");

        // ASSERT: Verificar que el turno se creó correctamente
        // AppointmentResponse tiene: Id, PatientName, DoctorName, Date, Status
        Assert.NotNull(response);
        Assert.Equal("BOOKED", response.Status);
        Assert.Equal(availabilitySlot.SlotDate, response.Date);

        // Verificar que el turno fue persistido en la base de datos en memoria
        var savedAppointment = await context.Set<Appointment>()
            .FirstOrDefaultAsync(a => a.AvailabilitySlotId == slotId);
        Assert.NotNull(savedAppointment);
        Assert.Equal(patientId, savedAppointment.PatientId);
        Assert.Equal("Consulta médica general", savedAppointment.Reason);
        Assert.Equal(AppointmentStatus.BOOKED, savedAppointment.Status);
    }
}
