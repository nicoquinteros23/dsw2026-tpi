using Dsw2026Tpi.Application.Dtos.Appointments;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // requiere el Token JWT que Flor armó
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    // Reservar un turno (POST /api/Appointments)
    [HttpPost]
    public async Task<IActionResult> Create(AppointmentRequest request)
    {
        var response = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetByPatient), new { dni = "" }, response);
    }

    // Ver turnos de un paciente (GET /api/Appointments/patient?dni=...)
    [HttpGet("patient")]
    public async Task<IActionResult> GetByPatient([FromQuery] string dni)
    {
        var result = await _service.GetByPatientDniAsync(dni);
        return Ok(result);
    }

    // Cancelar un turno (DELETE /api/Appointments/{id})
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _service.CancelAsync(id);
        return NoContent();
    }

    // Búsqueda avanzada para el Administrador (GET /api/Appointments/search)
    [HttpGet("search")]
    [Authorize(Roles = "ADMINISTRADOR")] // Solo el Admin puede usar este
    public async Task<IActionResult> Search(
        [FromQuery] Guid? specialityId,
        [FromQuery] Guid? doctorId,
        [FromQuery] string? dni,
        [FromQuery] DateTime? date,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.SearchAsync(specialityId, doctorId, dni, date, pageIndex, pageSize);
        return Ok(result);
    }
}