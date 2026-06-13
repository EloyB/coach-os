import apiClient from "@/lib/api-client";

export interface TrainerAvailabilityDto {
  id: string;
  trainerId: string;
  tennisClubId: string;
  tennisClubName: string;
  /** 0 = maandag ... 6 = zondag */
  dayOfWeek: number;
  /** "HH:mm" */
  startTime: string;
  /** "HH:mm" */
  endTime: string;
}

export interface CreateTrainerAvailabilityRequest {
  trainerId: string;
  tennisClubId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export async function getTrainerAvailabilities(): Promise<TrainerAvailabilityDto[]> {
  const { data } = await apiClient.get<TrainerAvailabilityDto[]>("/trainer-availabilities");
  return data;
}

export async function createTrainerAvailability(
  request: CreateTrainerAvailabilityRequest
): Promise<string> {
  const { data } = await apiClient.post<string>("/trainer-availabilities", request);
  return data;
}

export async function deleteTrainerAvailability(id: string): Promise<void> {
  await apiClient.delete(`/trainer-availabilities/${id}`);
}
