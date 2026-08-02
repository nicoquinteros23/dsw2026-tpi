using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly Dsw2026TpiDbContext _context;

    public AppointmentService(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<AppointmentResponse> CreateAsync(AppointmentRequest request)
    {
        // 1. Validar que no sea una fecha pasada
        if (request.Date < DateTime.Now)
            throw new Exception("No se pueden reservar turnos en fechas u horarios pasados.");

        // 2. Validar motivo (mínimo 5 caracteres según el PDF)
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5)
            throw new Exception("El motivo del turno es obligatorio y debe tener al menos 5 caracteres.");

        // 3. CONTROL DE CONCURRENCIA (Doble reserva)
        // Verificamos si ese bloque de horario (AvailabilitySlotId) ya tiene un turno RESERVADO (BOOKED)
        var isOccupied = await _context.Set<Appointment>()
            .AnyAsync(a => a.AvailabilitySlotId == request.AvailabilitySlotId && a.Status == AppointmentStatus.BOOKED);

        if (isOccupied)
            throw new Exception("Conflicto: Este turno ya ha sido reservado por otro paciente. Por favor, elija otro horario.");

        // 4. Crear el turno
        var appointment = new Appointment(request.PatientId, request.DoctorId, request.AvailabilitySlotId, request.Date, request.Reason);

        _context.Set<Appointment>().Add(appointment);
        await _context.SaveChangesAsync();

        return new AppointmentResponse
        {
            Id = appointment.Id,
            Date = appointment.Date,
            Status = appointment.Status.ToString()
        };
    }

    public async Task CancelAsync(Guid id)
    {
        var appointment = await _context.Set<Appointment>().FindAsync(id);

        if (appointment == null) throw new Exception("Turno no encontrado.");

        // REGLA DEL PDF: Solo se puede cancelar si está BOOKED
        if (appointment.Status != AppointmentStatus.BOOKED)
            throw new Exception("Solo se pueden cancelar turnos que estén en estado RESERVADO (BOOKED).");

        appointment.Cancel(); // Cambia el estado a CANCELLED
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AppointmentResponse>> GetByPatientDniAsync(string dni)
    {
        // Buscamos los turnos activos del paciente filtrando por su DNI
        // (Asumimos que la entidad Appointment tiene relación con Patient)
        return await _context.Set<Appointment>()
            .Include(a => a.Doctor)
            .Where(a => a.Status == AppointmentStatus.BOOKED)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.Date,
                DoctorName = a.Doctor.Name,
                Status = a.Status.ToString()
            }).ToListAsync();
    }

    public async Task<Pagination<AppointmentResponse>> SearchAsync(Guid? specialityId, Guid? doctorId, string? dni, DateTime? date, int pageIndex, int pageSize)
    {
        // Lógica para la búsqueda avanzada del Administrador con filtros opcionales
        var query = _context.Set<Appointment>()
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Speciality)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(a => a.Date.Date == date.Value.Date);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId);

        var total = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.Date,
                DoctorName = a.Doctor.Name,
                Status = a.Status.ToString()
            })
            .ToListAsync();

        return new Pagination<AppointmentResponse>(items, total, pageIndex, pageSize);
    }
}