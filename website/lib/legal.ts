import { readFileSync } from "node:fs";
import path from "node:path";
import { Marked, type Tokens } from "marked";

/**
 * Juridische documenten: één bron in `docs/legal` (markdown), gelezen bij de build.
 * Lokaal staat die map naast `website/`; in de Docker-build wordt ze naar `/docs/legal`
 * gekopieerd (zie `Dockerfile.prod`). `LEGAL_DOCS_DIR` overschrijft de locatie.
 */
const LEGAL_DIR =
  process.env.LEGAL_DOCS_DIR ?? path.join(process.cwd(), "..", "docs", "legal");

export type LegalSlug =
  | "algemene-voorwaarden-clubs"
  | "algemene-voorwaarden-inschrijving"
  | "privacyverklaring"
  | "verwerkersovereenkomst";

/** Route per document; ook gebruikt om onderlinge `.md`-verwijzingen te herschrijven. */
export const LEGAL_ROUTES: Record<LegalSlug, string> = {
  "algemene-voorwaarden-clubs": "/voorwaarden",
  "algemene-voorwaarden-inschrijving": "/spelersvoorwaarden",
  privacyverklaring: "/privacy",
  verwerkersovereenkomst: "/verwerkersovereenkomst",
};

export type LegalDocument = {
  title: string;
  html: string;
  version: string | null;
  effectiveFrom: string | null;
  updatedAt: string | null;
};

function slugify(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function metaValue(markdown: string, label: string): string | null {
  const match = markdown.match(new RegExp(`^\\*\\*${label}:\\*\\*\\s*(.+)$`, "m"));
  return match ? match[1].trim() : null;
}

export function loadLegalDocument(slug: LegalSlug): LegalDocument {
  const raw = readFileSync(path.join(LEGAL_DIR, `${slug}.md`), "utf8");

  const titleMatch = raw.match(/^#\s+(.+)$/m);
  const title = titleMatch ? titleMatch[1].trim() : slug;
  // Versie en data staan al in de paginakop; hier niet nogmaals tonen.
  const body = (titleMatch ? raw.replace(titleMatch[0], "") : raw).replace(
    /^\*\*(?:Versie|In werking vanaf|Laatst bijgewerkt):\*\*.*\n/gm,
    "",
  );

  const used = new Set<string>();
  const marked = new Marked({ gfm: true });
  marked.use({
    renderer: {
      heading({ tokens, depth }: Tokens.Heading) {
        const text = this.parser.parseInline(tokens);
        const plain = tokens.map((t) => ("text" in t ? t.text : "")).join("");
        let id = slugify(plain) || "sectie";
        for (let n = 2; used.has(id); n++) id = `${slugify(plain)}-${n}`;
        used.add(id);
        return `<h${depth} id="${id}"><a href="#${id}" class="legal-anchor" aria-label="Link naar deze sectie">${text}</a></h${depth}>\n`;
      },
      link({ href, title: linkTitle, tokens }: Tokens.Link) {
        const text = this.parser.parseInline(tokens);
        const target = href.replace(/^\.\//, "").replace(/\.md(#.*)?$/, "");
        const route = LEGAL_ROUTES[target as LegalSlug];
        const url = route ?? href;
        const external = /^https?:\/\//.test(url);
        const attrs = external ? ' rel="noopener noreferrer"' : "";
        const t = linkTitle ? ` title="${linkTitle}"` : "";
        return `<a href="${url}"${t}${attrs}>${text}</a>`;
      },
    },
  });

  return {
    title,
    html: marked.parse(body, { async: false }),
    version: metaValue(raw, "Versie"),
    effectiveFrom: metaValue(raw, "In werking vanaf"),
    updatedAt: metaValue(raw, "Laatst bijgewerkt"),
  };
}
