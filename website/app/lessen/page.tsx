import type { Metadata } from "next";
import { LessonSeriesIndex } from "@/components/sections/lesson-series-index";

const SITE_URL = "https://coach-os.be";
const PAGE_URL = `${SITE_URL}/lessen`;

export const metadata: Metadata = {
  title: "Lessen · CoachOS",
  description:
    "Lessenreeksen tennis en padel van clubs die met CoachOS werken. Bekijk niveau, prijs en beschikbare plekken — schrijf anoniem in via een publieke link.",
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
    title: "Lessen · CoachOS",
    description:
      "Lessenreeksen tennis en padel van clubs die met CoachOS werken.",
    siteName: "CoachOS",
  },
};

export default function LessenPage() {
  return <LessonSeriesIndex />;
}
