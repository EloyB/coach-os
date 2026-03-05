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
  createdAt: string;
}

export interface InviteTrainerRequest {
  firstName: string;
  lastName: string;
  email: string;
}

export interface AcceptInviteRequest {
  token: string;
  password: string;
}

export async function getTrainers(): Promise<TrainerDto[]> {
  const { data } = await apiClient.get<TrainerDto[]>("/trainers");
  return data;
}

export async function inviteTrainer(req: InviteTrainerRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/trainers/invite", req);
  return data;
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
