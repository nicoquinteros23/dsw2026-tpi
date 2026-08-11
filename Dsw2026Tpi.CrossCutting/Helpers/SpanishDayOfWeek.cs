using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class SpanishDayOfWeek
{
    private static readonly Dictionary<string, byte> NameToNumber = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DOMINGO"] = 0,
        ["LUNES"] = 1,
        ["MARTES"] = 2,
        ["MIÉRCOLES"] = 3,
        ["JUEVES"] = 4,
        ["VIERNES"] = 5,
        ["SÁBADO"] = 6,
    };

    private static readonly Dictionary<byte, string> NumberToName =
        NameToNumber.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static byte ToDayOfWeekNumber(string day)
    {
        if (day != null && NameToNumber.TryGetValue(day, out var number))
            return number;

        throw new ValidationException($"Día inválido: '{day}'. Valores permitidos: LUNES, MARTES, MIÉRCOLES, JUEVES, VIERNES, SÁBADO, DOMINGO.", "INVALID_DAY");
    }

    public static string ToSpanishName(byte dayOfWeek)
    {
        if (NumberToName.TryGetValue(dayOfWeek, out var name))
            return name;

        throw new ValidationException($"Día de la semana inválido: {dayOfWeek}.", "INVALID_DAY");
    }
}
