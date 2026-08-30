import apiClient from "@/lib/api-client";

export interface LessonSeriePriceDto {
  id: string;
  label: string;
  description: string | null;
  totalPrice: number;
  sortOrder: number;
  reusableKey: string | null;
}

export interface LessonSeriePriceRequest {
  label: string;
  description?: string | null;
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
