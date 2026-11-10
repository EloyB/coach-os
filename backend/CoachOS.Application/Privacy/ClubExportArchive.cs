using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Bouwt de ZIP van de clubexport: per tabel een CSV (UTF-8 met BOM, beschermd tegen
/// formule-injectie in Excel) en een JSON-bestand, plus een manifest.
/// </summary>
public static class ClubExportArchive
{
    public const int FormatVersion = 1;

    public static byte[] Build(Guid organizationId, IReadOnlyList<ExportTable> tables, DateTime generatedAtUtc)
    {
        using MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (ExportTable table in tables)
            {
                WriteEntry(zip, table.Name + ".csv", "﻿" + ToCsv(table));
                WriteEntry(zip, table.Name + ".json", ToJson(table));
            }

            WriteEntry(zip, "manifest.json", JsonSerializer.Serialize(new
            {
                organizationId,
                formatVersion = FormatVersion,
                generatedAtUtc,
                tables = tables.Select(t => new { name = t.Name, rows = t.Rows.Count }),
                scope = "Gegevens van deze organisatie, met expliciet gekozen kolommen.",
                exclusions = new[]
                {
                    "wachtwoorden en security stamps", "toegangs- en bevestigingstokens",
                    "OAuth- en Mollie-geheimen", "interne e-mailpayloads", "andere organisaties"
                },
                invoices = "Factuurnummers en -status zijn opgenomen; PDF's van de facturatieprovider niet."
            }));
        }

        return output.ToArray();
    }

    public static string ToCsv(ExportTable table)
    {
        StringBuilder csv = new();
        csv.AppendLine(string.Join(',', table.Columns.Select(EscapeCsv)));
        foreach (IReadOnlyList<object?> row in table.Rows)
            csv.AppendLine(string.Join(',', row.Select(value => EscapeCsv(FormatScalar(value)))));
        return csv.ToString();
    }

    /// <summary>
    /// Quote elke cel; waarden die als formule geïnterpreteerd kunnen worden (=, +, -, @, tab,
    /// CR/LF) krijgen een apostrof ervoor zodat een spreadsheet ze als tekst toont.
    /// </summary>
    public static string EscapeCsv(string value)
    {
        string trimmed = value.TrimStart();
        bool formulaLike = trimmed.Length > 0 && "=+-@".Contains(trimmed[0]);
        if (formulaLike || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string FormatScalar(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string ToJson(ExportTable table)
    {
        List<Dictionary<string, object?>> rows = table.Rows
            .Select(row => table.Columns.Select((column, index) => (column, value: row[index]))
                .ToDictionary(pair => pair.column, pair => pair.value))
            .ToList();
        return JsonSerializer.Serialize(rows);
    }

    private static void WriteEntry(ZipArchive zip, string name, string content)
    {
        using StreamWriter writer = new(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
