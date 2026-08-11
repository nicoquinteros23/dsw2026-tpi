namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : EntityBase
{
    public string Name { get; set; }
    public string LicenseNumber { get; set; }
    public Guid SpecialityId { get; set; }
    public Speciality Speciality { get; set; }
    public bool IsDeleted { get; set; } = false;

    #region Constructor for EF
    private Doctor() { }
    #endregion

    // Constructor que vamos a usar en el Service
    public Doctor(string name, string licenseNumber, Guid specialityId, Guid? id = null) : base(id)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        SpecialityId = specialityId;
        IsDeleted = false;
    }
}