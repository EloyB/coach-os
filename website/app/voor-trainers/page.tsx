import type { Metadata } from "next";
import { PersonaPage } from "@/components/sections/persona-page";
import { TRAINERS_PERSONA } from "@/content/personas";
import { pageMetadata } from "@/lib/seo";

export const metadata: Metadata = pageMetadata({
  path: `/${TRAINERS_PERSONA.slug}`,
  title: TRAINERS_PERSONA.metaTitle,
  description: TRAINERS_PERSONA.metaDescription,
});

export default function VoorTrainersPage() {
  return <PersonaPage persona={TRAINERS_PERSONA} />;
}
