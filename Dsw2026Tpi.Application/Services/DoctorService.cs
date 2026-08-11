using Dsw2026Tpi.Application.Dtos.Doctors;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly Dsw2026TpiDbContext _context;

    public DoctorService(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<Pagination<DoctorResponse>> GetAllAsync(int pageSize, int pageIndex, string? name = null)
    {
        var query = _context.Set<Doctor>()
            .Include(d => d.Speciality)
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(d => d.Name.Contains(name));

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                Name = d.Name,
                SpecialityName = d.Speciality.Name
            }).ToListAsync();

        return new Pagination<DoctorResponse>(pageSize, pageIndex, totalItems, items);
    }

    public async Task<DoctorResponse> GetByIdAsync(Guid id)
    {
        var d = await _context.Set<Doctor>()
            .Include(x => x.Speciality)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (d == null) throw new EntityNotFoundException("Doctor");

        return new DoctorResponse { Id = d.Id, Name = d.Name, SpecialityName = d.Speciality.Name };
    }

    public async Task<DoctorResponse> CreateAsync(DoctorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 100)
            throw new ValidationException("Nombre inválido (3-100 caracteres).", "VALIDATION_ERROR");

        var specialityExists = await _context.Set<Speciality>()
            .AnyAsync(s => s.Id == request.SpecialityId && !s.IsDeleted);

        if (!specialityExists)
            throw new BusinessRuleException("La especialidad asociada no existe.", "SPECIALITY_NOT_FOUND");

        var doctor = new Doctor(request.Name, request.SpecialityId);
        _context.Set<Doctor>().Add(doctor);
        await _context.SaveChangesAsync();

        return new DoctorResponse { Id = doctor.Id, Name = doctor.Name };
    }

    public async Task UpdateAsync(Guid id, DoctorRequest request)
    {
        var doctor = await _context.Set<Doctor>().FindAsync(id);
        if (doctor == null || doctor.IsDeleted) throw new EntityNotFoundException("Doctor");

        var specialityExists = await _context.Set<Speciality>()
            .AnyAsync(s => s.Id == request.SpecialityId && !s.IsDeleted);
        if (!specialityExists) throw new BusinessRuleException("La especialidad no existe.", "SPECIALITY_NOT_FOUND");

        doctor.Name = request.Name;
        doctor.SpecialityId = request.SpecialityId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var doctor = await _context.Set<Doctor>().FindAsync(id);
        if (doctor == null || doctor.IsDeleted) throw new EntityNotFoundException("Doctor");

        doctor.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}