import type { Metadata } from "next";
import { PersonaPage } from "@/components/sections/persona-page";
import { TENNIS_CLUBS_PERSONA } from "@/content/personas";
import { pageMetadata } from "@/lib/seo";

export const metadata: Metadata = pageMetadata({
  path: `/${TENNIS_CLUBS_PERSONA.slug}`,
  title: TENNIS_CLUBS_PERSONA.metaTitle,
  description: TENNIS_CLUBS_PERSONA.metaDescription,
});

export default function VoorTennisclubsPage() {
  return <PersonaPage persona={TENNIS_CLUBS_PERSONA} />;
}
