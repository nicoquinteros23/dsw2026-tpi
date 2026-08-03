using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Api.Services;

public class SignInService(
    SignInManager<ApplicationUser> signInManager,
    ILogger<SignInService> logger) : ISignInService
{
    public async Task<bool> CheckPassword(ApplicationUser user, string password)
    {
        var result = await signInManager.CheckPasswordSignInAsync(user, password, false);

        if (result.Succeeded)
        {
            logger.LogInformation("Inicio de sesión exitoso para el usuario: {Email}", user.Email);
        }
        else
        {
            logger.LogWarning("Intento fallido de inicio de sesión para el usuario: {Email}", user.Email);
        }

        return result.Succeeded;
    }
} 