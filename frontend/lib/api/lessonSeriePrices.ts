import apiClient from "@/lib/api-client";

/** 1 = volwassenen, 2 = jeugd. Spiegelt ParticipantCategory in de backend. */
export const PARTICIPANT_CATEGORIES = {
  Adult: 1,
  Youth: 2,
} as const;

export const CATEGORY_LABELS: Record<number, string> = {
  1: "Volwassenen",
  2: "Jeugd",
};

/** Groepsgroottes die de prijsmatrix aanbiedt, van groot naar klein. */
export const GROUP_SIZES = [4, 3, 2, 1] as const;

export interface LessonSeriePriceDto {
  id: string;
  category: number;
  categoryLabel: string;
  groupSize: number;
  /** TOTAALBEDRAG voor de hele groep van deze grootte — niet per persoon. */
  totalPrice: number;
}

export interface LessonSeriePriceRequest {
  category: number;
  groupSize: number;
  totalPrice: number;
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
