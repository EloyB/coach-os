import apiClient from "@/lib/api-client";

// ─── Response types ──────────────────────────────────────────────────────────

export interface AssignmentDetailsDto {
  assignmentId: string;
  seriesName: string;
  dayOfWeek: number; // 0=sunday..6=saturday
  startTime: string; // "HH:mm"
  endTime: string; // "HH:mm"
  courtName: string | null;
  studentName: string;
  pricePerPerson: number;
  totalPrice: number;
  isGroup: boolean;
  groupMemberNames: string[];
  status: "Pending" | "Confirmed" | "Declined";
  expiresAt: string; // ISO timestamp
}

export interface ConfirmResultDto {
  isConfirmed: boolean;
  checkoutUrl: string | null;
}

export interface AvailableSlotDto {
  weeklyTemplateEntryId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courtName: string | null;
  remainingCapacity: number;
}

export interface NonResponderDto {
  assignmentId: string;
  enrollmentId: string;
  studentName: string;
  studentEmail: string;
  studentPhone: string | null;
  isGroup: boolean;
  groupSize: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courtName: string | null;
  expiresAt: string;
  isExpired: boolean;
}

// ─── API calls ───────────────────────────────────────────────────────────────

export async function getAssignmentDetails(
  token: string
): Promise<AssignmentDetailsDto> {
  const { data } = await apiClient.get<AssignmentDetailsDto>(
    `/public/confirmation/${token}`
  );
  return data;
}

export async function confirmAssignment(
  token: string,
  paymentMethod: number,
  redirectUrl?: string
): Promise<ConfirmResultDto> {
  const { data } = await apiClient.post<ConfirmResultDto>(
    `/public/confirmation/${token}/confirm`,
    { paymentMethod, redirectUrl }
  );
  return data;
}

export async function declineAssignment(
  token: string
): Promise<{ availableSlots: AvailableSlotDto[] }> {
  const { data } = await apiClient.post<{
    availableSlots: AvailableSlotDto[];
  }>(`/public/confirmation/${token}/decline`);
  return data;
}

export async function getAvailableSlots(
  token: string
): Promise<{ slots: AvailableSlotDto[] }> {
  const { data } = await apiClient.get<{ slots: AvailableSlotDto[] }>(
    `/public/confirmation/${token}/available-slots`
  );
  return data;
}

export async function pickAlternativeSlot(
  token: string,
  weeklyTemplateEntryId: string,
  paymentMethod: number,
  redirectUrl?: string
): Promise<ConfirmResultDto> {
  const { data } = await apiClient.post<ConfirmResultDto>(
    `/public/confirmation/${token}/pick-alternative`,
    { weeklyTemplateEntryId, paymentMethod, redirectUrl }
  );
  return data;
}

export async function getNonResponders(
  seriesId: string
): Promise<NonResponderDto[]> {
  const { data } = await apiClient.get<NonResponderDto[]>(
    `/lessonseries/${seriesId}/planning/non-responders`
  );
  return data;
}
