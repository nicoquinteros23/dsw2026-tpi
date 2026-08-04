namespace Dsw2026Tpi.Application.Dtos.Appointments;

public class AppointmentRequest
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid AvailabilitySlotId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; } // Mínimo 5 caracteres
}