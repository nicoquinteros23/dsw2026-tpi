using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Domain;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Tests.Services;

/// <summary>
/// Pruebas unitarias para AppointmentService.
/// Valida las reglas de negocio del TPI: no fechas pasadas, motivo mínimo 5 chars,
/// control de doble reserva (concurrencia), creación exitosa y cancelación.
/// </summary>
public class AppointmentServiceTests
{
    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<Dsw2026Tpi.Data.Dsw2026TpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    /// <summary>
    /// Siembra Patient, Speciality y Doctor en el contexto y retorna sus IDs.
    /// </summary>
    private static async Task<(Guid patientId, Guid doctorId, Guid slotId)> SeedBaseDataAsync(
        TestDbContext context)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Dni = "12345678",
            FullName = "Paciente Test",
            Email = "paciente@test.com"
        };
        context.Patients.Add(patient);

        var speciality = new Speciality("Clínica Médica", "Medicina general y atención primaria");
        context.Set<Speciality>().Add(speciality);
        await context.SaveChangesAsync();

        var doctor = new Doctor("Dr. Test", "MP-22222", speciality.Id);
        context.Set<Doctor>().Add(doctor);
        await context.SaveChangesAsync();

        var rule = new AvailabilityRule(doctor.Id, 8, 2026, 1, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        context.Set<AvailabilityRule>().Add(rule);
        await context.SaveChangesAsync();

        var slotId = Guid.NewGuid();
        var slot = new AvailabilitySlot(rule.Id, DateTime.Now.AddDays(7), TimeSpan.FromHours(9), TimeSpan.FromHours(9.5), slotId);
        context.Set<AvailabilitySlot>().Add(slot);
        await context.SaveChangesAsync();

        return (patient.Id, doctor.Id, slotId);
    }

    // ──────────────────────────────────────────
    // PRUEBA 1 (Happy Path): Crear turno válido
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAppointmentSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);
        var service = new AppointmentService(context);

        var futureDate = DateTime.Now.AddDays(7);
        var request = new AppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AvailabilitySlotId = slotId,
            Date = futureDate,
            Reason = "Consulta médica de rutina anual"
        };

        var response = await service.CreateAsync(request, "paciente@test.com");

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("BOOKED", response.Status);
        Assert.Equal(futureDate, response.Date);

        var saved = await context.Set<Appointment>()
            .FirstOrDefaultAsync(a => a.Id == response.Id);
        Assert.NotNull(saved);
        Assert.Equal(patientId, saved.PatientId);
        Assert.Equal(AppointmentStatus.BOOKED, saved.Status);
    }

    // ──────────────────────────────────────────
    // PRUEBA 2: Fecha pasada
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithPastDate_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);
        var service = new AppointmentService(context);

        var request = new AppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AvailabilitySlotId = slotId,
            Date = DateTime.Now.AddDays(-1),
            Reason = "Consulta médica de rutina"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, "paciente@test.com"));
        Assert.Contains("pasados", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────
    // PRUEBA 3: Motivo demasiado corto
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("Doc")]
    public async Task CreateAsync_WithShortReason_ThrowsException(string shortReason)
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);
        var service = new AppointmentService(context);

        var request = new AppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AvailabilitySlotId = slotId,
            Date = DateTime.Now.AddDays(3),
            Reason = shortReason
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, "paciente@test.com"));
        Assert.Contains("motivo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────
    // PRUEBA 4: Doble reserva (control de concurrencia)
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithAlreadyBookedSlot_ThrowsConflictException()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);

        // Reserva previa con el mismo slot
        var existing = new Appointment(patientId, doctorId, slotId,
            DateTime.Now.AddDays(5), "Consulta médica previa");
        context.Set<Appointment>().Add(existing);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var request = new AppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AvailabilitySlotId = slotId,
            Date = DateTime.Now.AddDays(5),
            Reason = "Intento de doble reserva del mismo slot"
        };

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request, "paciente@test.com"));
        Assert.Contains("Conflicto", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────
    // PRUEBA 5: Cancelar un turno BOOKED
    // ──────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_BookedAppointment_ChangesStatusToCancelled()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);

        var appointment = new Appointment(patientId, doctorId, slotId,
            DateTime.Now.AddDays(3), "Consulta médica a cancelar");
        context.Set<Appointment>().Add(appointment);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);
        await service.CancelAsync(appointment.Id);

        var cancelled = await context.Set<Appointment>().FindAsync(appointment.Id);
        Assert.NotNull(cancelled);
        Assert.Equal(AppointmentStatus.CANCELLED, cancelled.Status);
    }

    // ──────────────────────────────────────────
    // PRUEBA 6: Cancelar turno ya CANCELLED
    // ──────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_AlreadyCancelledAppointment_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);

        var appointment = new Appointment(patientId, doctorId, slotId,
            DateTime.Now.AddDays(3), "Consulta ya cancelada");
        appointment.Cancel();
        context.Set<Appointment>().Add(appointment);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CancelAsync(appointment.Id));
        Assert.Contains("BOOKED", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────
    // PRUEBA 7: El slot cambia a BOOKED al reservar y AVAILABLE al cancelar
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAndCancel_UpdatesAvailabilitySlotStatusCorrectly()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);
        var service = new AppointmentService(context);

        // Validar que inicia en AVAILABLE
        var slotBefore = await context.Set<AvailabilitySlot>().FindAsync(slotId);
        Assert.NotNull(slotBefore);
        Assert.Equal("AVAILABLE", slotBefore.Status);

        var futureDate = DateTime.Now.AddDays(7);
        var request = new AppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AvailabilitySlotId = slotId,
            Date = futureDate,
            Reason = "Consulta de control anual médica"
        };

        // Crear reserva
        var response = await service.CreateAsync(request, "paciente@test.com");
        Assert.NotNull(response);

        // El slot debe cambiar a BOOKED
        var slotAfterBooking = await context.Set<AvailabilitySlot>().FindAsync(slotId);
        Assert.NotNull(slotAfterBooking);
        Assert.Equal("BOOKED", slotAfterBooking.Status);

        // Cancelar reserva
        await service.CancelAsync(response.Id);

        // El slot debe volver a AVAILABLE
        var slotAfterCancel = await context.Set<AvailabilitySlot>().FindAsync(slotId);
        Assert.NotNull(slotAfterCancel);
        Assert.Equal("AVAILABLE", slotAfterCancel.Status);
    }

    // ──────────────────────────────────────────
    // PRUEBA 8: Slot en el pasado lanza BusinessRuleException
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithPastSlot_ThrowsBusinessRuleException()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);
        
        // Modificar el slot sembrado para que esté en el pasado
        var slot = await context.Set<AvailabilitySlot>().FindAsync(slotId);
        Assert.NotNull(slot);
        slot.SlotDate = DateTime.Now.AddDays(-5);
        slot.StartTime = TimeSpan.FromHours(9);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var request = new AppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AvailabilitySlotId = slotId,
            Date = DateTime.Now.AddDays(7), // Request date is in future, but slot date is in past!
            Reason = "Consulta médica de rutina"
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request, "paciente@test.com"));
        Assert.Equal("APPOINTMENT_PAST_DATE", exception.Error.ErrorCode);
    }

    // ──────────────────────────────────────────
    // PRUEBA 9: GetByDateAsync – recupera los turnos del día
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetByDateAsync_ReturnsOnlyAppointmentsOnSpecificDate()
    {
        using var context = CreateInMemoryContext();
        var (patientId, doctorId, slotId) = await SeedBaseDataAsync(context);
        var targetDate = DateTime.Now.AddDays(7);

        // Turno del día objetivo
        var appointment1 = new Appointment(patientId, doctorId, slotId, targetDate, "Control de rutina");
        context.Set<Appointment>().Add(appointment1);

        // Turno de otro día diferente
        var anotherSlotId = Guid.NewGuid();
        var anotherAppointment = new Appointment(patientId, doctorId, anotherSlotId, DateTime.Now.AddDays(10), "Control de otro día");
        context.Set<Appointment>().Add(anotherAppointment);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);
        var result = await service.GetByDateAsync(targetDate);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(doctorId, appointment1.DoctorId);
    }
}
