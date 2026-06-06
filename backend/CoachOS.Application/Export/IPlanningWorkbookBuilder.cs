namespace CoachOS.Application.Export;

/// <summary>
/// Zet een <see cref="PlanningExportModel"/> om naar de bytes van een .xlsx-bestand.
/// Geïmplementeerd in de Infrastructure-laag (ClosedXML) zodat de Application-laag
/// vrij blijft van een externe Excel-dependency.
/// </summary>
public interface IPlanningWorkbookBuilder
{
    byte[] Build(PlanningExportModel model);
}
