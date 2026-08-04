using Xunit;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos.Specialities;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Tests.Services;

/// <summary>
/// Pruebas unitarias para SpecialityService
/// </summary>
public class SpecialityServiceTests
{
    /// <summary>
    /// Crea un DbContext en memoria para los tests con un nombre de BD único
    /// para evitar interferencias entre pruebas que se ejecuten en paralelo.
    /// </summary>
    private static Dsw2026TpiDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<Dsw2026TpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new Dsw2026TpiDbContext(options);
    }

    /// <summary>
    /// PRUEBA 3: Happy Path - Crear una especialidad válida y verificar que se agrega al repositorio
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithValidSpecialityRequest_CreatesSpecialitySuccessfully()
    {
        // ARRANGE: Contexto en memoria
        using var context = CreateInMemoryContext();
        var specialityService = new SpecialityService(context);

        var request = new SpecialityRequest
        {
            Name = "Cardiología",
            Description = "Especialidad médica dedicada al diagnóstico y tratamiento de enfermedades del corazón"
        };

        // ACT: Crear la especialidad
        var response = await specialityService.CreateAsync(request);

        // ASSERT: Verificar que la respuesta es correcta
        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);

        // Verificar que efectivamente fue persistida en la BD en memoria
        var saved = await context.Set<Speciality>()
            .FirstOrDefaultAsync(s => s.Name == request.Name);
        Assert.NotNull(saved);
        Assert.False(saved.IsDeleted);
    }

    /// <summary>
    /// Prueba adicional: Verificar que CreateAsync valida el nombre (debe tener 3-100 caracteres)
    /// </summary>
    [Theory]
    [InlineData("AB")]  // Muy corto (menos de 3)
    [InlineData("")]    // Vacío
    public async Task CreateAsync_WithInvalidName_ThrowsException(string invalidName)
    {
        // ARRANGE
        using var context = CreateInMemoryContext();
        var specialityService = new SpecialityService(context);

        var request = new SpecialityRequest
        {
            Name = invalidName,
            Description = "Una descripción válida con suficientes caracteres"
        };

        // ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(
            () => specialityService.CreateAsync(request)
        );
    }

    /// <summary>
    /// Prueba adicional: Verificar que CreateAsync valida la descripción (debe tener 10-100 caracteres)
    /// </summary>
    [Theory]
    [InlineData("Corta")]     // Muy corta (menos de 10)
    [InlineData("")]          // Vacía
    public async Task CreateAsync_WithInvalidDescription_ThrowsException(string invalidDescription)
    {
        // ARRANGE
        using var context = CreateInMemoryContext();
        var specialityService = new SpecialityService(context);

        var request = new SpecialityRequest
        {
            Name = "Especialidad Válida",
            Description = invalidDescription
        };

        // ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(
            () => specialityService.CreateAsync(request)
        );
    }
}
