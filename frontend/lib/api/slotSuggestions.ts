import apiClient from "@/lib/api-client";

export interface SuggestedTrainerDto {
  id: string;
  name: string;
}

export interface SlotSuggestionDto {
  /** 0 = maandag ... 6 = zondag */
  dayOfWeek: number;
  /** "HH:mm" */
  startTime: string;
  /** "HH:mm" */
  endTime: string;
  availableTrainerCount: number;
  trainers: SuggestedTrainerDto[];
  /** Aantal banen dat parallel gepland kan worden in dit venster. */
  suggestedParallelSlots: number;
}

export async function getSlotSuggestions(
  tennisClubId: string
): Promise<SlotSuggestionDto[]> {
  const { data } = await apiClient.get<SlotSuggestionDto[]>(
    `/tennisclubs/${tennisClubId}/slot-suggestions`
  );
  return data;
}
