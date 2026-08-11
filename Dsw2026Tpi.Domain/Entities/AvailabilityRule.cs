namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilityRule : EntityBase
{
    public Guid DoctorId { get; set; }
    public virtual Doctor Doctor { get; set; } = null!;
    public byte Month { get; set; }
    public short Year { get; set; }
    public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool Deleted { get; set; } = false;
    public virtual ICollection<AvailabilitySlot> AvailabilitySlots { get; set; } = new List<AvailabilitySlot>();

    #region Constructor for EF
    private AvailabilityRule() { }
    #endregion

    public AvailabilityRule(Guid doctorId, byte month, short year, byte dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? id = null) : base(id)
    {
        DoctorId = doctorId;
        Month = month;
        Year = year;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Deleted = false;
    }
}
