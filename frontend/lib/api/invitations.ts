import apiClient from "@/lib/api-client";

export interface PublicInvitationDto {
  /** 0 = Pending, 1 = Accepted, 2 = Declined */
  status: number;
  email: string;
  firstName: string | null;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  courtName: string;
  level: number | null;
  trainerName: string;
  notes: string | null;
  isCancelled: boolean;
  respondedAt: string | null;
}

export async function getPublicInvitation(
  token: string
): Promise<PublicInvitationDto> {
  const { data } = await apiClient.get<PublicInvitationDto>(
    `/public/invitations/${token}`
  );
  return data;
}

export async function acceptPublicInvitation(
  token: string
): Promise<PublicInvitationDto> {
  const { data } = await apiClient.post<PublicInvitationDto>(
    `/public/invitations/${token}/accept`
  );
  return data;
}

export async function declinePublicInvitation(
  token: string
): Promise<PublicInvitationDto> {
  const { data } = await apiClient.post<PublicInvitationDto>(
    `/public/invitations/${token}/decline`
  );
  return data;
}
