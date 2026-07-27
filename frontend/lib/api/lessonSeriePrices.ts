import apiClient from "@/lib/api-client";

export const PRICING_MODES = {
  FixedPerParticipant: 1,
  GroupSize: 2,
  TariffCategory: 3,
  ManualOption: 4,
} as const;

export const PRICING_MODE_LABELS: Record<number, string> = {
  1: "Vaste prijs per deelnemer",
  2: "Prijs per groepsgrootte",
  3: "Prijs per tariefcategorie",
  4: "Manueel gekozen optie",
};

export const PARTICIPANT_CATEGORIES = {
  Adult: 1,
  Youth: 2,
} as const;

export const CATEGORY_LABELS: Record<number, string> = {
  1: "Volwassenen",
  2: "Jeugd",
};

export const GROUP_SIZES = [4, 3, 2, 1] as const;

export interface LessonSeriePriceDto {
  id: string;
  label: string;
  description: string | null;
  mode: number;
  modeLabel: string;
  category: number | null;
  categoryLabel: string | null;
  groupSize: number | null;
  totalPrice: number;
  sortOrder: number;
  reusableKey: string | null;
}

export interface LessonSeriePriceRequest {
  label: string;
  description?: string | null;
  mode: number;
  category?: number | null;
  groupSize?: number | null;
  totalPrice: number;
  sortOrder: number;
  reusableKey?: string | null;
}

export async function getLessonSeriePrices(
  seriesId: string,
): Promise<LessonSeriePriceDto[]> {
  const { data } = await apiClient.get<LessonSeriePriceDto[]>(
    `/lessonseries/${seriesId}/prices`,
  );
  return data;
}

export async function saveLessonSeriePrices(
  seriesId: string,
  prices: LessonSeriePriceRequest[],
): Promise<LessonSeriePriceDto[]> {
  const { data } = await apiClient.put<LessonSeriePriceDto[]>(
    `/lessonseries/${seriesId}/prices`,
    { prices },
  );
  return data;
}
