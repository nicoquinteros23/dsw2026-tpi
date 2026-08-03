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
    public Guid DoctorId { get; private set; }
    public Doctor Doctor { get; private set; } // Relación con Médico
    public Guid AvailabilitySlotId { get; private set; }
    public DateTime Date { get; private set; }
    public string Reason { get; private set; }
    public AppointmentStatus Status { get; private set; }

    private Appointment() { }

    public Appointment(Guid patientId, Guid doctorId, Guid availabilitySlotId, DateTime date, string reason, Guid? id = null) : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        AvailabilitySlotId = availabilitySlotId;
        Date = date;
        Reason = reason;
        Status = AppointmentStatus.BOOKED;
    }

    public void Cancel() => Status = AppointmentStatus.CANCELLED;
}