using Dsw2026Tpi.Application.Dtos.Specialities;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialityService
{
    Task<Pagination<SpecialityResponse>> GetAllAsync(int pageSize, int pageIndex, string? name = null);
    Task<SpecialityResponse> GetByIdAsync(Guid id);
    Task<SpecialityResponse> CreateAsync(SpecialityRequest request);
    Task UpdateAsync(Guid id, SpecialityRequest request);
    Task DeleteAsync(Guid id); // Baja lógica
}