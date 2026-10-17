using System.Globalization;
using System.Text;
using Iroute.Commerce.Application.Exceptions;
using Iroute.Commerce.Application.Models;

namespace Iroute.Commerce.Application.Csv;

public sealed class CommerceCsvParser : ICommerceCsvParser
{
    private const int MaxReportedErrors = 50;

    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd/MM/yyyy", "ddMMyyyy", "yyyyMMdd"];

    // Columnas esperadas y su longitud máxima (igual que la tabla commerce).
    private static readonly (string Name, int MaxLength)[] Columns =
    [
        ("pc_processdate", 10),
        ("pc_codcomred", 20),
        ("pc_nomcomred", 150),
        ("pc_tipdoc", 10),
        ("pc_numdoc", 20),
        ("pc_direccion", 250),
        ("pc_ciudad", 100),
        ("pc_telefono", 20),
        ("pc_email", 150),
    ];

    public async Task<IReadOnlyList<CommerceRecord>> ParseAsync(Stream content, DateOnly expectedProcessDate, CancellationToken ct)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new ValidationException("El archivo se encuentra vacío.");

        var delimiter = headerLine.Count(c => c == ';') > headerLine.Count(c => c == ',') ? ';' : ',';
        var columnIndex = MapHeader(SplitLine(headerLine, delimiter));

        var records = new List<CommerceRecord>();
        var errors = new List<string>();
        var firstLineByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 1;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = SplitLine(line, delimiter);
            var record = TryBuildRecord(fields, columnIndex, lineNumber, errors);

            if (record is not null && ValidateBusinessKeys(record, expectedProcessDate, lineNumber, firstLineByCode, errors))
                records.Add(record);

            if (errors.Count >= MaxReportedErrors) break;
        }

        if (errors.Count > 0)
            throw new ValidationException("El archivo contiene errores de formato.", errors);

        if (records.Count == 0)
            throw new ValidationException("El archivo no contiene registros.");

        return records;
    }

    private static Dictionary<string, int> MapHeader(IReadOnlyList<string> header)
    {
        var map = header
            .Select((name, index) => (Name: name.Trim().ToLowerInvariant(), Index: index))
            .GroupBy(h => h.Name)
            .ToDictionary(g => g.Key, g => g.First().Index);

        var missing = Columns.Where(c => !map.ContainsKey(c.Name)).Select(c => c.Name).ToList();
        if (missing.Count > 0)
            throw new ValidationException(
                "La cabecera del archivo no tiene la estructura esperada.",
                [$"Columnas faltantes: {string.Join(", ", missing)}"]);

        return map;
    }

    private static CommerceRecord? TryBuildRecord(
        IReadOnlyList<string> fields, Dictionary<string, int> columnIndex, int lineNumber, List<string> errors)
    {
        var errorCountBefore = errors.Count;

        string? Get(string column)
        {
            var index = columnIndex[column];
            var value = index < fields.Count ? fields[index].Trim() : string.Empty;
            var maxLength = Columns.First(c => c.Name == column).MaxLength;

            if (value.Length > maxLength)
                errors.Add($"Línea {lineNumber}: '{column}' supera los {maxLength} caracteres.");

            return value.Length == 0 ? null : value;
        }

        var rawDate = Get("pc_processdate");
        if (!DateOnly.TryParseExact(rawDate, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var processDate))
            errors.Add($"Línea {lineNumber}: 'pc_processdate' inválida ({rawDate ?? "vacía"}).");

        var record = new CommerceRecord
        {
            PcProcessDate = processDate,
            PcCodComRed = Get("pc_codcomred"),
            PcNomComRed = Get("pc_nomcomred"),
            PcTipDoc = Get("pc_tipdoc"),
            PcNumDoc = Get("pc_numdoc"),
            PcDireccion = Get("pc_direccion"),
            PcCiudad = Get("pc_ciudad"),
            PcTelefono = Get("pc_telefono"),
            PcEmail = Get("pc_email"),
        };

        return errors.Count == errorCountBefore ? record : null;
    }

    /// <summary>
    /// Reglas que evitan cargas duplicadas o incoherentes: la fecha de la fila debe
    /// coincidir con la del nombre del archivo y el código de comercio es obligatorio
    /// y único dentro del archivo.
    /// </summary>
    private static bool ValidateBusinessKeys(
        CommerceRecord record, DateOnly expectedProcessDate, int lineNumber,
        Dictionary<string, int> firstLineByCode, List<string> errors)
    {
        var errorCountBefore = errors.Count;

        if (record.PcProcessDate != expectedProcessDate)
            errors.Add($"Línea {lineNumber}: 'pc_processdate' ({record.PcProcessDate:dd/MM/yyyy}) no coincide con la fecha del archivo ({expectedProcessDate:dd/MM/yyyy}).");

        if (record.PcCodComRed is null)
            errors.Add($"Línea {lineNumber}: 'pc_codcomred' es obligatorio.");
        else if (!firstLineByCode.TryAdd(record.PcCodComRed, lineNumber))
            errors.Add($"Línea {lineNumber}: el comercio '{record.PcCodComRed}' está repetido (línea {firstLineByCode[record.PcCodComRed]}).");

        return errors.Count == errorCountBefore;
    }

    /// <summary>
    /// Separa una línea CSV respetando campos entre comillas y comillas escapadas ("").
    /// </summary>
    internal static List<string> SplitLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else current.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == delimiter) { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
