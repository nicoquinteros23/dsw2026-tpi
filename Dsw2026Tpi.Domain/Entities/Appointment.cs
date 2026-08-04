namespace Dsw2026Tpi.Domain.Entities;

public enum AppointmentStatus
{
    BOOKED,
    CANCELLED,
    ATTENDED,
    NO_SHOW
}

public class Appointment : EntityBase
{
    public Guid PatientId { get; private set; }
    public virtual Patient Patient { get; set; }
    public Guid AvailabilitySlotId { get; private set; }
    public virtual AvailabilitySlot AvailabilitySlot { get; set; }
    public string Reason { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? AttendedAt { get; set; }

    private Appointment() { }

    public Appointment(Guid patientId, Guid availabilitySlotId, string reason, Guid? id = null) : base(id)
    {
        PatientId = patientId;
        AvailabilitySlotId = availabilitySlotId;
        Reason = reason;
        Status = AppointmentStatus.BOOKED;
    }

    public void Cancel() => Status = AppointmentStatus.CANCELLED;
}