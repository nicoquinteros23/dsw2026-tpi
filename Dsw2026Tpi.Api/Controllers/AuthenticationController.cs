using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/auth")]
public class AuthenticationController : AppController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("admin/register")]
   // [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterModel.Request request)
    {
        var result = await _authenticationService.Register(request);
        return Ok(result.Email);
    }

    [HttpPost("admin/login")]
    [AllowAnonymous]
    [EnableRateLimiting("AdminLoginPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginAdminModel.Request request)
    {
        var result = await _authenticationService.LoginAdmin(request);
        return Ok(result);
    }

    [HttpPost("patient/login")]
    [AllowAnonymous]
    [EnableRateLimiting("PatientLoginPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginPatient([FromBody] LoginPatientModel.Request request)
    {
        var result = await _authenticationService.LoginPatient(request);
        return Ok(result);
    }
}

