using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain;
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

    public async Task<AppointmentResponse> CreateAsync(AppointmentRequest request, string userEmail)
    {
        // 1. Buscar al paciente por su email
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Email == userEmail && !p.Deleted);
        
        if (patient == null)
            throw new Exception("El paciente asociado al usuario no existe o ha sido eliminado.");

        // 2. Validar que el AvailabilitySlot exista
        var availabilitySlot = await _context.Set<AvailabilitySlot>()
            .FirstOrDefaultAsync(s => s.Id == request.AvailabilitySlotId);
        
        if (availabilitySlot == null)
            throw new Exception("El horario disponible no existe.");

        // 3. Validar motivo (mínimo 5 caracteres según el PDF)
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5)
            throw new Exception("El motivo del turno es obligatorio y debe tener al menos 5 caracteres.");

        // 4. CONTROL DE CONCURRENCIA (Doble reserva)
        // Verificamos si ese bloque de horario (AvailabilitySlotId) ya tiene un turno RESERVADO (BOOKED)
        var isOccupied = await _context.Set<Appointment>()
            .AnyAsync(a => a.AvailabilitySlotId == request.AvailabilitySlotId && a.Status == AppointmentStatus.BOOKED);

        if (isOccupied)
            throw new Exception("Conflicto: Este turno ya ha sido reservado por otro paciente. Por favor, elija otro horario.");

        // 5. Crear el turno con el patientId obtenido del email
        var appointment = new Appointment(patient.Id, request.AvailabilitySlotId, request.Reason);

        _context.Set<Appointment>().Add(appointment);
        await _context.SaveChangesAsync();

        return new AppointmentResponse
        {
            Id = appointment.Id,
            Date = availabilitySlot.SlotDate,
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
        appointment.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AppointmentResponse>> GetByPatientDniAsync(string dni)
    {
        // Buscamos los turnos activos del paciente filtrando por su DNI
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Dni == dni && !p.Deleted);
        if (patient == null) return Enumerable.Empty<AppointmentResponse>();

        return await _context.Set<Appointment>()
            .Include(a => a.AvailabilitySlot)
            .ThenInclude(s => s.AvailabilityRule)
            .ThenInclude(ar => ar.Doctor)
            .Where(a => a.PatientId == patient.Id && a.Status == AppointmentStatus.BOOKED)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.AvailabilitySlot.SlotDate,
                DoctorName = a.AvailabilitySlot.AvailabilityRule.Doctor.Name,
                Status = a.Status.ToString()
            }).ToListAsync();
    }

    public async Task<Pagination<AppointmentResponse>> SearchAsync(Guid? specialityId, Guid? doctorId, string? dni, DateTime? date, int pageIndex, int pageSize)
    {
        // Lógica para la búsqueda avanzada del Administrador con filtros opcionales
        var query = _context.Set<Appointment>()
            .Include(a => a.AvailabilitySlot)
            .ThenInclude(s => s.AvailabilityRule)
            .ThenInclude(ar => ar.Doctor)
            .ThenInclude(d => d.Speciality)
            .AsQueryable();

        // Filtro por fecha del slot
        if (date.HasValue)
            query = query.Where(a => a.AvailabilitySlot.SlotDate.Date == date.Value.Date);

        // Filtro por médico
        if (doctorId.HasValue)
            query = query.Where(a => a.AvailabilitySlot.AvailabilityRule.DoctorId == doctorId);

        // Filtro por especialidad
        if (specialityId.HasValue)
            query = query.Where(a => a.AvailabilitySlot.AvailabilityRule.Doctor.SpecialityId == specialityId.Value);

        // Filtro por DNI del paciente
        if (!string.IsNullOrEmpty(dni))
            query = query.Where(a => _context.Patients.Any(p => p.Dni == dni && p.Id == a.PatientId && !p.Deleted));

        var total = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.AvailabilitySlot.SlotDate,
                DoctorName = a.AvailabilitySlot.AvailabilityRule.Doctor.Name,
                Status = a.Status.ToString()
            })
            .ToListAsync();

        return new Pagination<AppointmentResponse>(total, pageIndex, pageSize, items);
    }
}