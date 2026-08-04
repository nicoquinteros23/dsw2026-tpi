using Dsw2026Tpi.Application.Dtos.Availabilities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilityService
{
    Task<List<AvailabilityRuleResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request);
    Task<List<AvailabilityRuleResponse>> UpdateAvailabilityAsync(CreateAvailabilityRequest request);
    Task<DoctorAvailabilityResponse> GetDoctorAvailabilityAsync(Guid doctorId, int month, int year);
}
