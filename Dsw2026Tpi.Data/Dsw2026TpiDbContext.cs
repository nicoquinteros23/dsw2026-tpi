using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Dsw2026Tpi.Domain;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Data;

public class Dsw2026TpiDbContext: DbContext
{
    public Dsw2026TpiDbContext(DbContextOptions<Dsw2026TpiDbContext> options):
        base(options)
    {
    }
      
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Speciality> Specialities { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AvailabilityRule> AvailabilityRules { get; set; }
    public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
