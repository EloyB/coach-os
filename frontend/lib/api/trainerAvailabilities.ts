import apiClient from "@/lib/api-client";

export interface TrainerAvailabilityDto {
  id: string;
  trainerId: string;
  /** null = beschikbaar bij eender welke club */
  tennisClubId: string | null;
  /** null wanneer de beschikbaarheid voor eender welke club geldt */
  tennisClubName: string | null;
  /** 0 = maandag ... 6 = zondag */
  dayOfWeek: number;
  /** "HH:mm" */
  startTime: string;
  /** "HH:mm" */
  endTime: string;
}

export interface CreateTrainerAvailabilityRequest {
  trainerId: string;
  /** null = beschikbaar bij eender welke club */
  tennisClubId: string | null;
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

export async function updateTrainerAvailability(
  id: string,
  request: CreateTrainerAvailabilityRequest
): Promise<void> {
  await apiClient.put(`/trainer-availabilities/${id}`, request);
}

export async function deleteTrainerAvailability(id: string): Promise<void> {
  await apiClient.delete(`/trainer-availabilities/${id}`);
}

/** Eigen beschikbaarheid van de ingelogde trainer. De trainer komt uit het token. */
export interface SaveOwnTrainerAvailabilityRequest {
  /** null = beschikbaar bij eender welke club */
  tennisClubId: string | null;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export async function getOwnTrainerAvailabilities(): Promise<TrainerAvailabilityDto[]> {
  const { data } = await apiClient.get<TrainerAvailabilityDto[]>("/me/trainer-availabilities");
  return data;
}

export async function createOwnTrainerAvailability(
  request: SaveOwnTrainerAvailabilityRequest
): Promise<string> {
  const { data } = await apiClient.post<string>("/me/trainer-availabilities", request);
  return data;
}

export async function updateOwnTrainerAvailability(
  id: string,
  request: SaveOwnTrainerAvailabilityRequest
): Promise<void> {
  await apiClient.put(`/me/trainer-availabilities/${id}`, request);
}

export async function deleteOwnTrainerAvailability(id: string): Promise<void> {
  await apiClient.delete(`/me/trainer-availabilities/${id}`);
}
