namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilitySlot : EntityBase
{
    public Guid AvailabilityRuleId { get; set; }
    public virtual AvailabilityRule AvailabilityRule { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public bool Deleted { get; set; } = false;

    #region Constructor for EF
    private AvailabilitySlot() { }
    #endregion

    public AvailabilitySlot(Guid availabilityRuleId, DateTime slotDate, TimeSpan startTime, TimeSpan endTime, Guid? id = null) : base(id)
    {
        AvailabilityRuleId = availabilityRuleId;
        SlotDate = slotDate;
        StartTime = startTime;
        EndTime = endTime;
        Status = "AVAILABLE";
        Deleted = false;
    }
}
