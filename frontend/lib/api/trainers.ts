import apiClient from "@/lib/api-client";
import type { AuthResponse } from "@/lib/api/auth";

export interface TrainerDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  invitePending: boolean;
  lessonSeriesCount: number;
  currentWeekHoursBooked: number;
  weeklyCapacityHours: number;
  notes: string | null;
  createdAt: string;
}

export interface InviteTrainerRequest {
  firstName: string;
  lastName: string;
  email: string;
}

export interface UpdateTrainerRequest {
  firstName: string;
  lastName: string;
  weeklyCapacityHours: number;
  notes: string | null;
}

export interface AcceptInviteRequest {
  token: string;
  password: string;
}

export async function getTrainers(): Promise<TrainerDto[]> {
  const { data } = await apiClient.get<TrainerDto[]>("/trainers");
  return data;
}

export function isAssignableTrainer(t: TrainerDto): boolean {
  return t.isActive && !t.invitePending;
}

export async function inviteTrainer(req: InviteTrainerRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/trainers/invite", req);
  return data;
}

export async function updateTrainer(
  id: string,
  req: UpdateTrainerRequest,
): Promise<void> {
  await apiClient.put(`/trainers/${id}`, req);
}

export async function acceptInvite(req: AcceptInviteRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/trainers/accept-invite", req);
  return data;
}

export async function deactivateTrainer(id: string): Promise<void> {
  await apiClient.delete(`/trainers/${id}`);
}

export async function reassignTrainerSeries(fromId: string, toId: string): Promise<void> {
  await apiClient.post(`/trainers/${fromId}/reassign-series`, { toTrainerId: toId });
}

export async function removeTrainer(id: string): Promise<void> {
  await apiClient.delete(`/trainers/${id}/remove`);
}

export async function resendTrainerInvite(id: string): Promise<void> {
  await apiClient.post(`/trainers/${id}/resend-invite`);
}
