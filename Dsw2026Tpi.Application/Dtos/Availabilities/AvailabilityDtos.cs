namespace Dsw2026Tpi.Application.Dtos.Availabilities;

public class DaySlot
{
    public byte DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class CreateAvailabilityRequest
{
    public Guid DoctorId { get; set; }
    public short Year { get; set; }
    public byte Month { get; set; }
    public List<DaySlot> Days { get; set; } = new();
}

public class AvailabilityRuleResponse
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public byte Month { get; set; }
    public short Year { get; set; }
    public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class AvailabilitySlotResponse
{
    public Guid Id { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; }
}

public class DoctorAvailabilityResponse
{
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public List<AvailabilitySlotResponse> AvailableSlots { get; set; } = new();
}
