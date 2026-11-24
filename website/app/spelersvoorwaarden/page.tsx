import type { Metadata } from "next";
import { LegalDocumentPage } from "@/components/sections/legal-document";

export const metadata: Metadata = {
  title: "Voorwaarden voor inschrijving",
  description: "Algemene voorwaarden voor spelers die zich inschrijven voor een lessenreeks via CoachOS.",
  alternates: { canonical: "/spelersvoorwaarden" },
};

export default function Page() {
  return <LegalDocumentPage slug="algemene-voorwaarden-inschrijving" />;
}
