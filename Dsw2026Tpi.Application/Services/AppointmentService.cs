using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
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

    public async Task<AppointmentResponse> CreateAsync(AppointmentRequest request)
    {
        // 1. Buscar al paciente por su DNI (fuente de verdad del turno)
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Dni == request.Patient.Dni && !p.Deleted);

        if (patient == null)
            throw new EntityNotFoundException("Patient");

        var availabilitySlot = await _context.Set<AvailabilitySlot>().FindAsync(request.AvailabilityId);
        if (availabilitySlot == null || availabilitySlot.Deleted)
            throw new EntityNotFoundException("AvailabilitySlot");

        var slotDateTime = availabilitySlot.SlotDate.Date.Add(availabilitySlot.StartTime);
        if (slotDateTime < DateTime.Now)
            throw new BusinessRuleException("No se pueden reservar turnos en fechas u horarios pasados.", "APPOINTMENT_PAST_DATE");

        // 2. Validar motivo (mínimo 5 caracteres según el PDF)
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5)
            throw new ValidationException("El motivo del turno es obligatorio y debe tener al menos 5 caracteres.", "VALIDATION_ERROR");

        // 3. CONTROL DE CONCURRENCIA (Doble reserva)
        var isOccupied = await _context.Set<Appointment>()
            .AnyAsync(a => a.AvailabilitySlotId == request.AvailabilityId && a.Status == AppointmentStatus.BOOKED);

        if (isOccupied)
            throw new ConflictException("APPOINTMENT_CONFLICT", "Conflicto: Este turno ya ha sido reservado por otro paciente. Por favor, elija otro horario.");

        availabilitySlot.Status = "BOOKED";

        // 4. Crear el turno; la fecha/hora sale del AvailabilitySlot encontrado
        var appointment = new Appointment(patient.Id, request.DoctorId, request.AvailabilityId, slotDateTime, request.Reason);

        _context.Set<Appointment>().Add(appointment);
        await _context.SaveChangesAsync();

        return new AppointmentResponse
        {
            Id = appointment.Id,
            PatientName = patient.FullName,
            PatientDni = patient.Dni,
            Date = appointment.Date,
            Status = appointment.Status.ToString()
        };
    }

    public async Task CancelAsync(Guid id)
    {
        var appointment = await _context.Set<Appointment>().FindAsync(id);

        if (appointment == null) throw new EntityNotFoundException("Appointment");

        // REGLA DEL PDF: Solo se puede cancelar si está BOOKED
        if (appointment.Status != AppointmentStatus.BOOKED)
            throw new BusinessRuleException("Solo se pueden cancelar turnos que estén en estado RESERVADO (BOOKED).", "INVALID_APPOINTMENT_STATUS");

        appointment.Cancel(); // Cambia el estado a CANCELLED

        var slot = await _context.Set<AvailabilitySlot>().FindAsync(appointment.AvailabilitySlotId);
        if (slot != null)
        {
            slot.Status = "AVAILABLE";
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AppointmentResponse>> GetByPatientDniAsync(string dni)
    {
        // Buscamos los turnos activos del paciente filtrando por su DNI
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Dni == dni && !p.Deleted);
        if (patient == null) return Enumerable.Empty<AppointmentResponse>();

        return await _context.Set<Appointment>()
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patient.Id && a.Status == AppointmentStatus.BOOKED)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.Date,
                DoctorName = a.Doctor.Name,
                Status = a.Status.ToString(),
                PatientName = patient.FullName,
                PatientDni = patient.Dni
            }).ToListAsync();
    }

    public async Task<Pagination<AppointmentResponse>> SearchAsync(Guid? specialtyId, Guid? doctorId, string? dni, DateTime? date, int pageIndex, int pageSize)
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

        if (specialtyId.HasValue)
            query = query.Where(a => a.Doctor.SpecialityId == specialtyId.Value);

        if (!string.IsNullOrEmpty(dni))
            query = query.Where(a => _context.Patients.Any(p => p.Dni == dni && p.Id == a.PatientId && !p.Deleted));

        var total = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Join(_context.Patients, a => a.PatientId, p => p.Id, (a, p) => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.Date,
                DoctorName = a.Doctor.Name,
                Status = a.Status.ToString(),
                PatientName = p.FullName,
                PatientDni = p.Dni,
                SpecialityName = a.Doctor.Speciality.Name
            })
            .ToListAsync();

        return new Pagination<AppointmentResponse>(pageSize, pageIndex, total, items);
    }

    public async Task<IEnumerable<AppointmentResponse>> GetByDateAsync(DateTime date)
    {
        return await _context.Set<Appointment>()
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Speciality)
            .Where(a => a.Date.Date == date.Date)
            .Join(_context.Patients, a => a.PatientId, p => p.Id, (a, p) => new AppointmentResponse
            {
                Id = a.Id,
                Date = a.Date,
                DoctorName = a.Doctor.Name,
                Status = a.Status.ToString(),
                PatientName = p.FullName,
                PatientDni = p.Dni,
                SpecialityName = a.Doctor.Speciality.Name
            })
            .ToListAsync();
    }
}
