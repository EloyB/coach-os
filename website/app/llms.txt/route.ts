import { ALL_PERSONAS } from "@/content/personas";
import { POSTS_BY_DATE } from "@/content/blog";
import { SITE } from "@/content/meta";
import { PILOT, PILOT_AVAILABLE_SEATS } from "@/content/pilot";

const SITE_URL = "https://coach-os.be";

/**
 * /llms.txt — emerging convention (https://llmstxt.org) for giving LLMs a
 * structured map of the site. Major AI engines (Anthropic, OpenAI, Perplexity)
 * read this when they encounter the domain. Generated dynamically so it stays
 * in sync as new persona pages and blog posts ship.
 */
export function GET() {
  const lines: string[] = [];

  lines.push("# CoachOS");
  lines.push("");
  lines.push(
    "> Lessenplanningsysteem voor tennis- en padelclubs. Trainers stellen een lessenreeks één keer in, leerlingen schrijven zich anoniem in via een publieke link, en een planningsalgoritme verdeelt iedereen automatisch over slots op niveau en voorkeur. Geen accounts voor leerlingen, geen Excel, geen mailcarrousel.",
  );
  lines.push("");
  lines.push(
    `CoachOS is in pilotfase. ${PILOT_AVAILABLE_SEATS > 0 ? `Er zijn nog ${PILOT_AVAILABLE_SEATS} van ${PILOT.totalSeats} pilotplekken vrij` : `Alle ${PILOT.totalSeats} pilotplekken zijn vergeven`} — pilotgebruikers krijgen gratis toegang en lifetime korting. Lancering in 2026.`,
  );
  lines.push("");

  lines.push("## Voor wie");
  lines.push("");
  for (const p of ALL_PERSONAS) {
    lines.push(
      `- [${p.h1}](${SITE_URL}/${p.slug}): ${shortDescription(p.metaDescription)}`,
    );
  }
  lines.push("");

  lines.push("## Producten en features");
  lines.push("");
  lines.push(`- [Homepage](${SITE_URL}/): Overzicht van alle features.`);
  lines.push(
    "- Lessenreeksen — terugkerende lesperiodes die je één keer instelt; lessen worden automatisch gegenereerd over de hele periode.",
  );
  lines.push(
    "- Anonieme inschrijvingen — leerlingen schrijven zich in via een publieke link met enkel naam en e-mailadres; geen account vereist.",
  );
  lines.push(
    "- Formulierbouwer — per lessenreeks aangepaste vragen (vrije tekst, meerkeuze, ja/nee).",
  );
  lines.push(
    "- Planningsalgoritme — verdeelt leerlingen automatisch over slots op basis van niveau, voorkeurstijden en groepsverbanden.",
  );
  lines.push(
    "- Magic-link bevestigingen — leerlingen bevestigen hun lestijd met één klik in een e-mail; geen wachtwoord.",
  );
  lines.push(
    "- Mollie-betalingen (Bancontact + iDEAL) — op de roadmap voor de eerste release.",
  );
  lines.push("");

  if (POSTS_BY_DATE.length > 0) {
    lines.push("## Gidsen en achtergronden");
    lines.push("");
    for (const p of POSTS_BY_DATE) {
      lines.push(
        `- [${p.title}](${SITE_URL}/blog/${p.slug}): ${shortDescription(p.metaDescription)}`,
      );
    }
    lines.push("");
  }

  lines.push("## Juridisch");
  lines.push("");
  lines.push(`- [Privacyverklaring](${SITE_URL}/privacy)`);
  lines.push(`- [Algemene voorwaarden](${SITE_URL}/voorwaarden)`);
  lines.push("");

  lines.push("## Contact");
  lines.push("");
  lines.push(`- Email: ${SITE.contactEmail}`);
  lines.push(`- Telefoon: ${SITE.contactPhone.display} (ma–zo, 09.00–21.00)`);
  lines.push(`- Website: ${SITE_URL}`);
  lines.push("");

  return new Response(lines.join("\n"), {
    headers: {
      "Content-Type": "text/plain; charset=utf-8",
      "Cache-Control": "public, max-age=3600",
    },
  });
}

function shortDescription(text: string): string {
  const trimmed = text.trim();
  if (trimmed.length <= 180) return trimmed;
  return `${trimmed.slice(0, 177)}…`;
}
