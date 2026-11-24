import type { Metadata } from "next";
import { LegalDocumentPage } from "@/components/sections/legal-document";

export const metadata: Metadata = {
  title: "Privacy",
  description: "Privacyverklaring van CoachOS.",
  alternates: { canonical: "/privacy" },
};

export default function Page() {
  return <LegalDocumentPage slug="privacyverklaring" />;
}
