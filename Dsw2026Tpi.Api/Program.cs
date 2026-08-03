using System.Threading.RateLimiting;
using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

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
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });

            var adminPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:AdminLogin:PermitLimit");
            var adminWindowSeconds = builder.Configuration.GetValue<int>("RateLimiting:AdminLogin:WindowInSeconds");
            var patientPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PatientLogin:PermitLimit");
            var patientWindowSeconds = builder.Configuration.GetValue<int>("RateLimiting:PatientLogin:WindowInSeconds");

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("AdminLoginPolicy", httpContext =>
                {
                    var remoteIp = httpContext.Connection.RemoteIpAddress;
                    var ip = remoteIp == null || System.Net.IPAddress.IsLoopback(remoteIp) ? "127.0.0.1" : remoteIp.ToString();

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = adminPermitLimit > 0 ? adminPermitLimit : 5,
                            Window = TimeSpan.FromSeconds(adminWindowSeconds > 0 ? adminWindowSeconds : 60),
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("PatientLoginPolicy", httpContext =>
                {
                    var remoteIp = httpContext.Connection.RemoteIpAddress;
                    var ip = remoteIp == null || System.Net.IPAddress.IsLoopback(remoteIp) ? "127.0.0.1" : remoteIp.ToString();

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = patientPermitLimit > 0 ? patientPermitLimit : 10,
                            Window = TimeSpan.FromSeconds(patientWindowSeconds > 0 ? patientWindowSeconds : 60),
                            QueueLimit = 0
                        });
                });
            });

            builder.Services.AddAppAuthentication(builder.Configuration);
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddApplicationPersistence(builder.Configuration);
            builder.Services.AddAppCors(builder.Configuration);
            builder.Services.AddAppDependencies();
            builder.Services.AddControllers();
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

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseRouting();
            app.UseCors();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

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
