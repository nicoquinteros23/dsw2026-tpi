using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Specialities;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Tests.Services;

/// <summary>
/// Pruebas unitarias para SpecialityService.
/// Valida las reglas de negocio del TPI: nombre (3-100 chars), descripción (10-100 chars),
/// persistencia correcta y baja lógica.
/// </summary>
public class SpecialityServiceTests
{
    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<Dsw2026Tpi.Data.Dsw2026TpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    // ──────────────────────────────────────────
    // PRUEBA 1 (Happy Path): Crear especialidad válida
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesSpecialitySuccessfully()
    {
        using var context = CreateInMemoryContext();
        var service = new SpecialityService(context);

        var request = new SpecialityRequest
        {
            Name = "Cardiología",
            Description = "Especialidad médica dedicada al diagnóstico y tratamiento de enfermedades del corazón"
        };

        var response = await service.CreateAsync(request);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);

        var saved = await context.Set<Speciality>()
            .FirstOrDefaultAsync(s => s.Name == request.Name);
        Assert.NotNull(saved);
        Assert.False(saved.IsDeleted);
    }

    // ──────────────────────────────────────────
    // PRUEBA 2: Validación de nombre inválido
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("AB")]
    [InlineData("")]
    public async Task CreateAsync_WithInvalidName_ThrowsException(string invalidName)
    {
        using var context = CreateInMemoryContext();
        var service = new SpecialityService(context);

        var request = new SpecialityRequest
        {
            Name = invalidName,
            Description = "Una descripción válida con suficientes caracteres"
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    // ──────────────────────────────────────────
    // PRUEBA 3: Validación de descripción inválida
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("Corta")]
    [InlineData("")]
    public async Task CreateAsync_WithInvalidDescription_ThrowsException(string invalidDescription)
    {
        using var context = CreateInMemoryContext();
        var service = new SpecialityService(context);

        var request = new SpecialityRequest
        {
            Name = "Especialidad Válida",
            Description = invalidDescription
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    // ──────────────────────────────────────────
    // PRUEBA 4: Baja lógica (DeleteAsync)
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingSpeciality_MarksAsDeleted()
    {
        using var context = CreateInMemoryContext();
        var speciality = new Speciality("Neurología", "Especialidad dedicada al sistema nervioso");
        context.Set<Speciality>().Add(speciality);
        await context.SaveChangesAsync();

        var service = new SpecialityService(context);
        await service.DeleteAsync(speciality.Id);

        var deleted = await context.Set<Speciality>().FindAsync(speciality.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    // ──────────────────────────────────────────
    // PRUEBA 5: GetByIdAsync – especialidad inexistente
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var service = new SpecialityService(context);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    // ──────────────────────────────────────────
    // PRUEBA 6: GetAllAsync – con paginación y filtro opcional de nombre
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithPaginationAndFilter_ReturnsCorrectPage()
    {
        using var context = CreateInMemoryContext();
        context.Set<Speciality>().Add(new Speciality("Pediatría", "Atención para niños"));
        context.Set<Speciality>().Add(new Speciality("Traumatología", "Huesos y articulaciones"));
        context.Set<Speciality>().Add(new Speciality("Cardiología", "Salud cardiovascular"));
        
        var deletedSpec = new Speciality("Oftalmología", "Cuidado ocular");
        deletedSpec.IsDeleted = true;
        context.Set<Speciality>().Add(deletedSpec);
        await context.SaveChangesAsync();

        var service = new SpecialityService(context);

        // 1. Obtener todas las no borradas
        var pageAll = await service.GetAllAsync(pageSize: 10, pageIndex: 0);
        Assert.NotNull(pageAll);
        Assert.Equal(3, pageAll.Total);
        Assert.Equal(10, pageAll.PageSize);
        Assert.Equal(0, pageAll.PageIndex);
        Assert.Equal(3, pageAll.Data.Count());

        // 2. Probar filtro por nombre
        var pageFilter = await service.GetAllAsync(pageSize: 5, pageIndex: 0, name: "ia");
        Assert.NotNull(pageFilter);
        // Pediatría y Cardiología contienen "ia" (Traumatología también, "Traumatología" -> "ia" en "logía", wait: "Pediatría" has "ía", "Traumatología" has "ía", "Cardiología" has "ía", wait: "Cardiología" y "Pediatría" y "Traumatología" all have "ía", wait, the letters "ia" in Spanish: "Pediatría" has "ia" at the end, "Traumatología" has "ia" at the end, "Cardiología" has "ia" at the end. Oh, yes! All 3 contain "ia". Let's search for "Ped" to match exactly 1, or "Cardio" to match exactly 1).
        
        var pageFilterPed = await service.GetAllAsync(pageSize: 5, pageIndex: 0, name: "Ped");
        Assert.Equal(1, pageFilterPed.Total);
        Assert.Single(pageFilterPed.Data);
        Assert.Equal("Pediatría", pageFilterPed.Data.First().Name);
    }
}
