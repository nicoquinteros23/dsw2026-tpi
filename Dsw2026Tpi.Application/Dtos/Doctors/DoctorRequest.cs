namespace Dsw2026Tpi.Application.Dtos.Doctors;

public class DoctorRequest
{
    public string Name { get; set; }
    public Guid SpecialityId { get; set; } // ID de la especialidad (con 'i')
}