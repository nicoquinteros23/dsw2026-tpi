using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Serilog;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;

namespace Dsw2026Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Iniciando aplicación Dsw2026Tpi.Api");

            var builder = WebApplication.CreateBuilder(args);

            builder.AddSerilogConfiguration();
            builder.Services.AddAppIdentity();
            builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });
            builder.Services.AddAppAuthentication(builder.Configuration);
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddApplicationPersistence(builder.Configuration);
            builder.Services.AddAppCors(builder.Configuration);

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter("AdminLoginPolicy", policy =>
                {
                    policy.PermitLimit = 5;
                    policy.Window = TimeSpan.FromMinutes(1);
                    policy.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("PatientLoginPolicy", policy =>
                {
                    policy.PermitLimit = 10;
                    policy.Window = TimeSpan.FromMinutes(1);
                    policy.QueueLimit = 0;
                });
            });

            builder.Services.AddScoped<ISpecialityService, SpecialityService>();
            builder.Services.AddScoped<IDoctorService, DoctorService>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();

            builder.Services.AddAppDependencies();

            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
                            .ToList();

                        var response = new
                        {
                            errorCode = "BAD_REQUEST",
                            message = "La solicitud contiene errores de validación.",
                            details = errors
                        };

                        return new BadRequestObjectResult(response);
                    };
                });

            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // Ejecutar migraciones y crear datos iniciales
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var authDb = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
                    var appDb = scope.ServiceProvider.GetRequiredService<Dsw2026TpiDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    // Ejecutar migraciones
                    await authDb.Database.MigrateAsync();
                    await appDb.Database.MigrateAsync();

                    // Crear roles si no existen
                    if (!await roleManager.RoleExistsAsync(Roles.Administrator))
                    {
                        await roleManager.CreateAsync(new IdentityRole(Roles.Administrator));
                    }

                    if (!await roleManager.RoleExistsAsync(Roles.Patient))
                    {
                        await roleManager.CreateAsync(new IdentityRole(Roles.Patient));
                    }

                    // Crear usuario administrador por defecto
                    var adminEmail = "admin@dsw2026.com";
                    var adminPassword = "Admin123";
                    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

                    if (existingAdmin == null)
                    {
                        var adminUser = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(adminUser, adminPassword);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, Roles.Administrator);
                            Log.Information("Usuario administrador por defecto creado: {Email}", adminEmail);
                        }
                        else
                        {
                            Log.Warning("No se pudo crear el usuario administrador por defecto. Errores: {@Errors}", result.Errors);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error durante la inicialización de la base de datos");
                    throw;
                }
            }

            app.UseSerilogRequestLogging();

            if (app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors();
            app.UseRateLimiter();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();
            app.MapHealthChecks("/health-check");

            Log.Information("Aplicación iniciada correctamente");

            await app.RunAsync();
        }
        catch (HostAbortedException)
        {
            Log.Information("El host fue abortado (normal durante migraciones de EF Core)");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "La aplicación falló al iniciar");
            throw;
        }
        finally
        {
            Log.Information("Cerrando aplicación");
            await Log.CloseAndFlushAsync();
        }
    }
}
