using Dsw2026Tpi.Application.Dtos.Specialities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialityService
{
    Task<IEnumerable<SpecialityResponse>> GetAllAsync();
    Task<SpecialityResponse> GetByIdAsync(Guid id);
    Task<SpecialityResponse> CreateAsync(SpecialityRequest request);
    Task UpdateAsync(Guid id, SpecialityRequest request);
    Task DeleteAsync(Guid id); // Baja lógica
}