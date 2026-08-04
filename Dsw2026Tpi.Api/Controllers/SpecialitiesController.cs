using Dsw2026Tpi.Application.Dtos.Specialities;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protegido: requiere Token
public class SpecialitiesController : ControllerBase
{
    private readonly ISpecialityService _service;

    public SpecialitiesController(ISpecialityService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous] 
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR")] // Solo el Admin puede crear
    public async Task<IActionResult> Create(SpecialityRequest request)
    {
        var response = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), new { id = response.Id }, response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMINISTRADOR")] // Solo el Admin puede dar de baja
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}