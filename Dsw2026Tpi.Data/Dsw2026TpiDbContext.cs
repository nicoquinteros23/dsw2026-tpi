using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Dsw2026Tpi.Domain;

namespace Dsw2026Tpi.Data;

public class Dsw2026TpiDbContext: DbContext
{
    public Dsw2026TpiDbContext(DbContextOptions<Dsw2026TpiDbContext> options):
        base(options)
    {
    }
      
    public DbSet<Patient> Patients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
