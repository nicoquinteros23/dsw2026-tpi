namespace Dsw2026Tpi.Application.Dtos.Availabilities;

public class AvailabilityDayRequest
{
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // "HH:mm"
    public string EndTime { get; set; } = string.Empty;   // "HH:mm"
}

public class CreateAvailabilityRequest
{
    public Guid DoctorId { get; set; }
    public List<AvailabilityDayRequest> Days { get; set; } = new();
}

public class AvailabilityRuleResponse
{
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class DoctorAvailabilityDayResponse
{
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
