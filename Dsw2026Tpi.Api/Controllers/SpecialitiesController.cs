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
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, [FromQuery] string? name = null)
    {
        var result = await _service.GetAllAsync(pageSize, pageIndex, name);
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

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SpecialityRequest request)
    {
        await _service.UpdateAsync(id, request);
        var response = await _service.GetByIdAsync(id);
        return Ok(response);
    }
}