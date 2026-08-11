using Dsw2026Tpi.Application.Dtos.Specialities;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services;

public class SpecialityService : ISpecialityService
{
    private readonly Dsw2026TpiDbContext _context;

    public SpecialityService(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<Pagination<SpecialityResponse>> GetAllAsync(int pageSize, int pageIndex, string? name = null)
    {
        // REGLA DEL PDF: No listar las que tengan baja lógica (IsDeleted)
        var query = _context.Set<Speciality>()
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(s => s.Name.Contains(name));

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(s => new SpecialityResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            }).ToListAsync();

        return new Pagination<SpecialityResponse>(pageSize, pageIndex, totalItems, items);
    }

    public async Task<SpecialityResponse> GetByIdAsync(Guid id)
    {
        var speciality = await _context.Set<Speciality>()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (speciality == null) throw new EntityNotFoundException("Speciality");

        return new SpecialityResponse
        {
            Id = speciality.Id,
            Name = speciality.Name,
            Description = speciality.Description
        };
    }

    public async Task<SpecialityResponse> CreateAsync(SpecialityRequest request)
    {
        // VALIDACIONES DEL PDF: Nombre (3-100) y Descripción (10-100)
        ValidateRequest(request);

        var speciality = new Speciality(request.Name, request.Description);

        _context.Set<Speciality>().Add(speciality);
        await _context.SaveChangesAsync();

        return new SpecialityResponse
        {
            Id = speciality.Id,
            Name = speciality.Name,
            Description = speciality.Description
        };
    }

    public async Task UpdateAsync(Guid id, SpecialityRequest request)
    {
        var speciality = await _context.Set<Speciality>().FindAsync(id);

        if (speciality == null || speciality.IsDeleted)
            throw new EntityNotFoundException("Speciality");

        ValidateRequest(request);

        // Actualizamos los campos (usando reflexión o manual según tu EntityBase)
        // Aquí asumo que podés setearlos directamente
        typeof(Speciality).GetProperty("Name")?.SetValue(speciality, request.Name);
        typeof(Speciality).GetProperty("Description")?.SetValue(speciality, request.Description);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var speciality = await _context.Set<Speciality>().FindAsync(id);

        if (speciality == null) throw new EntityNotFoundException("Speciality");

        // REGLA DEL PDF: Baja lógica, nunca borrar de verdad
        speciality.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    // Método privado para no repetir las validaciones del PDF
    private void ValidateRequest(SpecialityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 100)
            throw new ValidationException("Nombre obligatorio, entre 3 y 100 caracteres.", "VALIDATION_ERROR");

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length < 10 || request.Description.Length > 100)
            throw new ValidationException("Descripción obligatoria, entre 10 y 100 caracteres.", "VALIDATION_ERROR");
    }
}