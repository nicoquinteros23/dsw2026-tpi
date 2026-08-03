using System.ComponentModel.DataAnnotations;

namespace Dsw2026Tpi.Application.Dtos;

public record LoginAdminModel
{
    public record Request(
        [property: Required(ErrorMessage = "El email es obligatorio.")]
        [property: EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        string Email,

        [property: Required(ErrorMessage = "La contraseña es obligatoria.")]
        [property: MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        string Password
    );
    public record Response(string? Token, string? Role);
}

public record LoginPatientModel
{
    public record Request(
        [property: Required(ErrorMessage = "El email es obligatorio.")]
        [property: EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        string Email,

        [property: Required(ErrorMessage = "El DNI es obligatorio.")]
        [property: Range(1000000, 99999999, ErrorMessage = "El DNI debe tener entre 7 y 8 dígitos.")]
        long Dni
    );

    public record Response(string? Token, string? Role);
}
