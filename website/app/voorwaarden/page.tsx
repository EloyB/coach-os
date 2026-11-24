import type { Metadata } from "next";
import { LegalDocumentPage } from "@/components/sections/legal-document";

export const metadata: Metadata = {
  title: "Algemene voorwaarden",
  description: "Algemene voorwaarden van CoachOS voor clubs en scholen.",
  alternates: { canonical: "/voorwaarden" },
};

export default function Page() {
  return <LegalDocumentPage slug="algemene-voorwaarden-clubs" />;
}
