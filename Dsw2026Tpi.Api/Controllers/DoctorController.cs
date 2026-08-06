using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Dtos.Doctors; 
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/doctors")]
[Authorize(Policy = Policies.AdminPolicy)]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;
    private readonly IAvailabilityService _availabilityService;

    public DoctorController(IDoctorService service, IAvailabilityService availabilityService)
    {
        _service = service;
        _availabilityService = availabilityService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, [FromQuery] string? name = null)
    {
        
        var doctors = await _service.GetAllAsync(pageSize, pageIndex, name);
        return Ok(doctors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorRequest request)
    {
        var result = await _service.CreateAsync(request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
     }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DoctorRequest request)
    {
        await _service.UpdateAsync(id, request);
        var response = await _service.GetByIdAsync(id);
        return Ok(response);
    }

    [HttpGet("{id}/availabilities")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailabilities(Guid id, [FromQuery] int month, [FromQuery] int year)
    {
        var availabilities = await _availabilityService.GetDoctorAvailabilityAsync(id, month, year);
        return Ok(availabilities);
    }
}