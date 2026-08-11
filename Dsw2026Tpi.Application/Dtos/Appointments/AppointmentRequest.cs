using System.Text.Json.Serialization;

namespace Dsw2026Tpi.Application.Dtos.Appointments;

public class AppointmentRequest
{
    public Guid DoctorId { get; set; }
    public Guid AvailabilityId { get; set; }
    public AppointmentPatientRequest Patient { get; set; }
    public string Reason { get; set; } // Mínimo 5 caracteres
}

public class AppointmentPatientRequest
{
    [JsonConverter(typeof(Dsw2026Tpi.Application.Dtos.DniJsonConverter))]
    public string Dni { get; set; }
}
