using Microsoft.OpenApi.Models; 

namespace Dsw2026Tpi.Api.Configurations;

public static class SwaggerConfigurationExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(o =>
        {
            const string schemeId = "Bearer";

            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Desarrollo de Software 2026",
                Version = "v1",
            });

            o.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Ingrese el token JWT con el prefijo 'Bearer ' (ej: Bearer eyJhbG...)",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = schemeId
                        }
                    },
                    new List<string>()
                }
            });

            // Configura nombres únicos para schemas con tipos anidados
            o.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        });
        return services;
    }
}