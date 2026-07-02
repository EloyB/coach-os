import type { Metadata } from "next";
import { PersonaPage } from "@/components/sections/persona-page";
import { PADEL_CLUBS_PERSONA } from "@/content/personas";

const SITE_URL = "https://coach-os.be";
const PAGE_URL = `${SITE_URL}/${PADEL_CLUBS_PERSONA.slug}`;

export const metadata: Metadata = {
  title: PADEL_CLUBS_PERSONA.metaTitle,
  description: PADEL_CLUBS_PERSONA.metaDescription,
  alternates: {
    canonical: PAGE_URL,
    languages: {
      "nl-BE": PAGE_URL,
      "nl-NL": PAGE_URL,
      "x-default": PAGE_URL,
    },
  },
  openGraph: {
    type: "website",
    locale: "nl_BE",
    alternateLocale: ["nl_NL"],
    url: PAGE_URL,
    title: PADEL_CLUBS_PERSONA.metaTitle,
    description: PADEL_CLUBS_PERSONA.metaDescription,
    siteName: "CoachOS",
  },
};

export default function VoorPadelscholenPage() {
  return <PersonaPage persona={PADEL_CLUBS_PERSONA} />;
}
