namespace CoachOS.Application.Export;

/// <summary>
/// Zet een <see cref="CampExportModel"/> om naar de bytes van een .xlsx-bestand.
/// Geïmplementeerd in Infrastructure (ClosedXML).
/// </summary>
public interface ICampWorkbookBuilder
{
    byte[] Build(CampExportModel model);
}
