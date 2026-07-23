using Dsw2026Tpi.Data;
using Dsw2026Tpi.Data.Extensions;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Api.Configurations;

public static class PersistenceConfigurationExtensions
{
    public static IServiceCollection AddApplicationPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        //Obtener cadena de conexión desde appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        //Agregar contexto (O/RM) y utilizar SQL Server para DB
        services.AddDbContext<Dsw2026TpiDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDbContext<AuthenticationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseSeeding((c, t) =>
            {
                c.Seedwork<IdentityRole>("Sources\\roles.json");

                var context = (AuthenticationDbContext)c;
                if (!context.Set<ApplicationUser>().Any(u => u.Email == "admin@sistema.com"))
                {
                    var admin = new ApplicationUser
                    {
                        Id = "A0A0A0A0-B0B0-C0C0-D0D0-E0E0E0E0E0E0",
                        UserName = "admin@sistema.com",
                        Email = "admin@sistema.com",
                        NormalizedUserName = "ADMIN@SISTEMA.COM",
                        NormalizedEmail = "ADMIN@SISTEMA.COM",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var hasher = new PasswordHasher<ApplicationUser>();
                    admin.PasswordHash = hasher.HashPassword(admin, "Admin123*");

                    context.Set<ApplicationUser>().Add(admin);

                    context.Set<IdentityUserRole<string>>().Add(new IdentityUserRole<string>
                    {
                        UserId = admin.Id,
                        RoleId = "2E0D9BDF-0041-4589-B721-C495FA7C39D8"
                    });

                    context.SaveChanges();
                }
            });
        });
        return services;
    }
}
