import type { Metadata } from "next";
import { BlogIndex } from "@/components/sections/blog-index";

const SITE_URL = "https://coach-os.be";
const PAGE_URL = `${SITE_URL}/blog`;

export const metadata: Metadata = {
  title: "Blog · CoachOS",
  description:
    "Gidsen en achtergronden over lessenplanning voor tennis- en padelclubs: AVG-conform inschrijven, seizoensplanning, magic-link bevestigingen.",
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
    title: "Blog · CoachOS",
    description:
      "Praktische gidsen over lessenplanning voor tennis- en padelclubs.",
    siteName: "CoachOS",
  },
};

export default function BlogPage() {
  return <BlogIndex />;
}
