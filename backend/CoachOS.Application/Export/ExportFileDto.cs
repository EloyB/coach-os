namespace CoachOS.Application.Export;

/// <summary>
/// Een gegenereerd downloadbaar bestand (bytes + metadata) klaar om als
/// HTTP file-response teruggegeven te worden.
/// </summary>
public record ExportFileDto(byte[] Content, string FileName, string ContentType);
