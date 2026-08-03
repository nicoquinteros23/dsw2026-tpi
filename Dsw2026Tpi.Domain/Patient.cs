using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Domain
{
    public class Patient : EntityBase
    {
        public string Dni { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefono { get; set; }
        public Guid ApplicationUserId { get; set; }
        public bool Deleted { get; set; } = false;
    }
}
