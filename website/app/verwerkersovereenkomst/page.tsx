import type { Metadata } from "next";
import { LegalDocumentPage } from "@/components/sections/legal-document";

export const metadata: Metadata = {
  title: "Verwerkersovereenkomst",
  description: "Verwerkersovereenkomst van CoachOS.",
  alternates: { canonical: "/verwerkersovereenkomst" },
};

export default function Page() {
  return <LegalDocumentPage slug="verwerkersovereenkomst" />;
}
