using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    /// <summary>
    /// Reservar un turno (POST /api/Appointments)
    /// Requiere un paciente autenticado; el paciente del turno se identifica por
    /// el DNI enviado en el body (patient.dni), fuente de verdad según la consigna.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Paciente")]
    [EnableRateLimiting("AppointmentBookingPolicy")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] AppointmentRequest request)
    {
        var response = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetByPatient), new { dni = "" }, response);
    }

    /// <summary>
    /// Turnos del día para el Administrador (GET /api/Appointments?date=YYYY-MM-DD)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
    {
        var result = await _service.GetByDateAsync(date);
        return Ok(result);
    }

    /// <summary>
    /// Ver turnos de un paciente (GET /api/Appointments/patient?dni=...)
    /// </summary>
    [HttpGet("patient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient([FromQuery] string dni)
    {
        var result = await _service.GetByPatientDniAsync(dni);
        return Ok(result);
    }

    /// <summary>
    /// Cancelar un turno (DELETE /api/Appointments/{id})
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _service.CancelAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Búsqueda avanzada para el Administrador (GET /api/Appointments/search)
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? specialtyId,
        [FromQuery] Guid? doctorId,
        [FromQuery] string? dni,
        [FromQuery] DateTime? date,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.SearchAsync(specialtyId, doctorId, dni, date, pageIndex, pageSize);
        return Ok(result);
    }
}