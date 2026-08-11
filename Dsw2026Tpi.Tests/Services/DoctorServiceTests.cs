using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Doctors;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Tests.Services;

/// <summary>
/// Pruebas unitarias para DoctorService.
/// Valida las reglas de negocio del TPI: nombre (3-100 chars),
/// especialidad existente, creación exitosa y baja lógica.
/// </summary>
public class DoctorServiceTests
{
    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<Dsw2026Tpi.Data.Dsw2026TpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private static async Task<Speciality> SeedSpecialityAsync(TestDbContext context)
    {
        var speciality = new Speciality("Cardiología", "Especialidad del corazón y sistema circulatorio");
        context.Set<Speciality>().Add(speciality);
        await context.SaveChangesAsync();
        return speciality;
    }

    // ──────────────────────────────────────────
    // PRUEBA 1 (Happy Path): Crear médico válido
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesDoctorSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var speciality = await SeedSpecialityAsync(context);
        var service = new DoctorService(context);

        var request = new DoctorRequest
        {
            Name = "Dr. Juan Pérez",
            SpecialityId = speciality.Id
        };

        var response = await service.CreateAsync(request);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(request.Name, response.Name);

        var saved = await context.Set<Doctor>()
            .FirstOrDefaultAsync(d => d.Id == response.Id);
        Assert.NotNull(saved);
        Assert.False(saved.IsDeleted);
        Assert.Equal(speciality.Id, saved.SpecialityId);
    }

    // ──────────────────────────────────────────
    // PRUEBA 2: Nombre de médico inválido
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("AB")]
    [InlineData("")]
    public async Task CreateAsync_WithInvalidName_ThrowsException(string invalidName)
    {
        using var context = CreateInMemoryContext();
        var speciality = await SeedSpecialityAsync(context);
        var service = new DoctorService(context);

        var request = new DoctorRequest
        {
            Name = invalidName,
            SpecialityId = speciality.Id
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    // ──────────────────────────────────────────
    // PRUEBA 3: Especialidad inexistente al crear médico
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNonExistentSpeciality_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var service = new DoctorService(context);

        var request = new DoctorRequest
        {
            Name = "Dr. Ana García",
            SpecialityId = Guid.NewGuid()
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
        Assert.Contains("especialidad", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────
    // PRUEBA 4: Baja lógica (DeleteAsync)
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingDoctor_MarksAsDeleted()
    {
        using var context = CreateInMemoryContext();
        var speciality = await SeedSpecialityAsync(context);
        var doctor = new Doctor("Dr. Carlos López", speciality.Id);
        context.Set<Doctor>().Add(doctor);
        await context.SaveChangesAsync();

        var service = new DoctorService(context);
        await service.DeleteAsync(doctor.Id);

        var deleted = await context.Set<Doctor>().FindAsync(doctor.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    // ──────────────────────────────────────────
    // PRUEBA 5: GetByIdAsync – médico inexistente
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var service = new DoctorService(context);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }
}
