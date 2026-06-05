/**
 * Triggert een browser-download voor een in-memory blob (bv. een geëxporteerd
 * Excel-bestand dat via de API als binary is opgehaald).
 */
export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
