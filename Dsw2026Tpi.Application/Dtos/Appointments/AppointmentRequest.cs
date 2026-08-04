namespace Dsw2026Tpi.Application.Dtos.Appointments;

public class AppointmentRequest
{
    public Guid AvailabilitySlotId { get; set; }
    public string Reason { get; set; } // Mínimo 5 caracteres
}