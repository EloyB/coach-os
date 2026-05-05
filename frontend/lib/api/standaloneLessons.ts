import apiClient from "@/lib/api-client";

export interface InvitationDto {
  id: string;
  email: string;
  firstName: string | null;
  /** 0 = Pending, 1 = Accepted, 2 = Declined */
  status: number;
  invitationSentAt: string;
  respondedAt: string | null;
}

export interface StandaloneLessonListItemDto {
  id: string;
  date: string;
  startTime: string;
  endTime: string;
  courtName: string;
  level: number | null;
  trainerId: string | null;
  trainerName: string | null;
  maxParticipants: number;
  invitedCount: number;
  acceptedCount: number;
  isCancelled: boolean;
}

export interface StandaloneLessonDetailDto {
  id: string;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  courtName: string;
  level: number | null;
  trainerId: string | null;
  trainerName: string | null;
  maxParticipants: number;
  notes: string | null;
  isCancelled: boolean;
  invitations: InvitationDto[];
}

export interface CreateStandaloneLessonRequest {
  date: string;
  startTime: string;
  durationMinutes: number;
  courtName: string;
  level: number | null;
  trainerId: string;
  maxParticipants: number;
  notes: string | null;
  participantEmails: string[];
}

export interface AddInvitationsRequest {
  emails: string[];
}

export const STANDALONE_LESSON_LEVELS: Record<number, string> = {
  1: "Beginner",
  2: "Gemiddeld",
  3: "Gevorderd",
};

export const INVITATION_STATUS_LABEL: Record<number, string> = {
  0: "Uitgenodigd",
  1: "Bevestigd",
  2: "Afgemeld",
};

export const ALLOWED_DURATIONS = [30, 45, 60, 90, 120] as const;

export async function getStandaloneLessons(): Promise<StandaloneLessonListItemDto[]> {
  const { data } = await apiClient.get<StandaloneLessonListItemDto[]>(
    "/standalone-lessons"
  );
  return data;
}

export async function getStandaloneLesson(
  id: string
): Promise<StandaloneLessonDetailDto> {
  const { data } = await apiClient.get<StandaloneLessonDetailDto>(
    `/standalone-lessons/${id}`
  );
  return data;
}

export async function createStandaloneLesson(
  request: CreateStandaloneLessonRequest
): Promise<string> {
  const { data } = await apiClient.post<string>("/standalone-lessons", request);
  return data;
}

export async function cancelStandaloneLesson(
  id: string,
  reason?: string
): Promise<void> {
  await apiClient.post(
    `/standalone-lessons/${id}/cancel`,
    reason ? { reason } : {}
  );
}

export async function addInvitations(
  lessonId: string,
  emails: string[]
): Promise<void> {
  await apiClient.post(`/standalone-lessons/${lessonId}/invitations`, {
    emails,
  } satisfies AddInvitationsRequest);
}

export async function resendInvitation(
  lessonId: string,
  invitationId: string
): Promise<void> {
  await apiClient.post(
    `/standalone-lessons/${lessonId}/invitations/${invitationId}/resend`
  );
}
