using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> CreateAsync(AppointmentRequest request);
    Task<IEnumerable<AppointmentResponse>> GetByPatientDniAsync(string dni);
    Task CancelAsync(Guid id);
    // Búsqueda avanzada Admin
    Task<Pagination<AppointmentResponse>> SearchAsync(Guid? specialtyId, Guid? doctorId, string? dni, DateTime? date, int pageIndex, int pageSize);
    Task<IEnumerable<AppointmentResponse>> GetByDateAsync(DateTime date);
}