namespace Dsw2026Tpi.Application.Dtos.Doctors;

public class DoctorResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string LicenseNumber { get; set; }
    public DoctorSpecialityResponse Specialty { get; set; }
}

public class DoctorSpecialityResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
