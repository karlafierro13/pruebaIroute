using System.Globalization;
using System.Text.RegularExpressions;

namespace Iroute.Commerce.Application.Csv;

/// <summary>
/// Valida el nombre del archivo: commerce_DDMMYYYY.csv (con fecha real).
/// </summary>
public static partial class CommerceFileName
{
    [GeneratedRegex(@"^commerce_(?<date>\d{8})\.csv$", RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();

    public static bool IsValid(string? fileName) => TryGetDate(fileName, out _);

    /// <summary>Obtiene la fecha DDMMYYYY del nombre del archivo.</summary>
    public static bool TryGetDate(string? fileName, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var match = Pattern().Match(Path.GetFileName(fileName));
        return match.Success && DateOnly.TryParseExact(
            match.Groups["date"].Value, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
