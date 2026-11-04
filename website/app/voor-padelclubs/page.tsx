import type { Metadata } from "next";
import { PersonaPage } from "@/components/sections/persona-page";
import { PADEL_CLUBS_PERSONA } from "@/content/personas";
import { pageMetadata } from "@/lib/seo";

export const metadata: Metadata = pageMetadata({
  path: `/${PADEL_CLUBS_PERSONA.slug}`,
  title: PADEL_CLUBS_PERSONA.metaTitle,
  description: PADEL_CLUBS_PERSONA.metaDescription,
});

export default function VoorPadelclubsPage() {
  return <PersonaPage persona={PADEL_CLUBS_PERSONA} />;
}
