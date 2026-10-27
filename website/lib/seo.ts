import type { Metadata } from "next";

export const SITE_URL = "https://coach-os.be";

const BRAND = "CoachOS";

interface PageMetadataInput {
  /** Pad zonder domein, bv. "/voor-padelclubs". */
  path: string;
  /**
   * Paginatitel zonder merknaam: de titeltemplate in de root layout voegt
   * " · CoachOS" toe. Zo komt de merknaam precies één keer voor.
   */
  title: string;
  description: string;
  /** Afwijkende beschrijving voor OpenGraph en Twitter; standaard `description`. */
  socialDescription?: string;
  /** Houd de pagina uit de zoekresultaten (links blijven gevolgd). */
  noindex?: boolean;
}

/**
 * Metadata voor een losse pagina, inclusief OpenGraph en Twitter. Een pagina die
 * zelf `openGraph` zet, erft de Twitter-tags van de root layout. Door die hier per
 * pagina te zetten krijgt elke pagina zijn eigen, specifieke tekst.
 */
export function pageMetadata({
  path,
  title,
  description,
  socialDescription,
  noindex = false,
}: PageMetadataInput): Metadata {
  const url = `${SITE_URL}${path}`;
  const socialTitle = `${title} · ${BRAND}`;
  const socialText = socialDescription ?? description;

  return {
    title,
    description,
    alternates: {
      canonical: url,
      languages: { "nl-BE": url, "nl-NL": url, "x-default": url },
    },
    openGraph: {
      type: "website",
      locale: "nl_BE",
      alternateLocale: ["nl_NL"],
      url,
      title: socialTitle,
      description: socialText,
      siteName: BRAND,
    },
    twitter: {
      card: "summary_large_image",
      title: socialTitle,
      description: socialText,
    },
    ...(noindex ? { robots: { index: false, follow: true } } : {}),
  };
}
