using Dsw2026Tpi.Application.Dtos.Availabilities;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/availabilities")]
[Authorize(Policy = Policies.AdminPolicy)]
public class AvailabilitiesController : AppController
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilitiesController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>
    /// Crea disponibilidades para un médico para el resto del mes en curso.
    /// Genera automáticamente slots de 30 minutos basados en los días y horarios proporcionados.
    /// </summary>
    /// <param name="request">Contiene doctorId y array de días (en español) con horarios</param>
    /// <returns>Lista de reglas de disponibilidad creadas</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateAvailabilityRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _availabilityService.CreateAvailabilityAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Actualiza las disponibilidades del mes en curso de un médico.
    /// Elimina los slots no reservados y reemplaza las reglas.
    /// </summary>
    /// <param name="request">Contiene doctorId y array de días (en español) con horarios</param>
    /// <returns>Lista de reglas de disponibilidad actualizadas</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update([FromBody] CreateAvailabilityRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _availabilityService.UpdateAvailabilityAsync(request);
        return Ok(result);
    }
}
