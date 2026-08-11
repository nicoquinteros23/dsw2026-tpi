namespace Dsw2026Tpi.Application.Dtos.Appointments;

public class AppointmentResponse
{
    public Guid Id { get; set; }
    public string PatientName { get; set; }
    public string PatientDni { get; set; }
    public string DoctorName { get; set; }
    public string SpecialityName { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
}
