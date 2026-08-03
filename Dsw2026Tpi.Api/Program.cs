using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
