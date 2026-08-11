using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Tests;

/// <summary>
/// DbContext extendido para pruebas unitarias.
/// El DbContext de producción sólo expone DbSet&lt;Patient&gt;,
/// pero los Services acceden a Appointment, Doctor y Speciality
/// a través de context.Set&lt;T&gt;(). Este contexto de test registra
/// esas entidades en el modelo para que funcionen con InMemory.
/// </summary>
public class TestDbContext : Dsw2026TpiDbContext
{
    public TestDbContext(DbContextOptions<Dsw2026TpiDbContext> options) : base(options) { }

    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Doctor> Doctors { get; set; } = null!;
    public DbSet<Speciality> Specialities { get; set; } = null!;
}
