namespace CoachOS.Domain.Models;

/// <summary>
/// Eén tabel uit de clubexport: vaste, expliciet gekozen kolommen en de rijen in die volgorde.
/// </summary>
public sealed record ExportTable(string Name, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows);
