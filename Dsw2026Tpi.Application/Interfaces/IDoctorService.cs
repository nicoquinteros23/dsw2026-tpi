using Dsw2026Tpi.Application.Dtos.Doctors;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    
    Task<Pagination<DoctorResponse>> GetAllAsync(int pageSize, int pageIndex, string? name = null);

    // Obtener un médico por su ID
    Task<DoctorResponse> GetByIdAsync(Guid id);

    // Crear un médico nuevo (validando que la especialidad exista)
    Task<DoctorResponse> CreateAsync(DoctorRequest request);

    // Actualizar datos de un médico
    Task UpdateAsync(Guid id, DoctorRequest request);

   
    Task DeleteAsync(Guid id);
}