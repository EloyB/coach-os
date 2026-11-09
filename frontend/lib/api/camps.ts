import apiClient from "@/lib/api-client";
import publicApiClient from "@/lib/public-api-client";

export interface CampDayTrainerDto {
  trainerId: string;
  trainerName: string;
  startTime: string; // "HH:mm"
  endTime: string;
}

/** Voormiddag en namiddag van een kampdag; null = dat dagdeel bestaat niet op die dag. */
export interface CampDayPartsDto {
  morningStart: string | null; // "HH:mm"
  morningEnd: string | null;
  afternoonStart: string | null;
  afternoonEnd: string | null;
}

export interface CampDayDto extends CampDayPartsDto {
  id: string;
  date: string; // "yyyy-MM-dd"
  trainers: CampDayTrainerDto[];
}

/** Publieke kampdag: dagdelen plus of ze het maximum aantal deelnemers bereikt hebben. */
export interface PublicCampDayDto extends CampDayPartsDto {
  id: string;
  date: string;
  morningFull: boolean;
  afternoonFull: boolean;
}

/** Gekozen dagdelen van één kampdag. */
export interface CampDaySelection {
  campDayId: string;
  morning: boolean;
  afternoon: boolean;
}

export interface CampDto {
  id: string;
  name: string;
  tennisClubId: string;
  tennisClubName: string;
  level: number | null;
  dayPrice: number;
  halfDayPrice: number;
  allowHalfDays: boolean;
  startDate: string;
  endDate: string;
  maxParticipants: number | null;
  participantCount: number;
  dayCount: number;
  isActive: boolean;
}

export interface CampDetailDto {
  id: string;
  name: string;
  description: string | null;
  tennisClubId: string;
  tennisClubName: string;
  level: number | null;
  dayPrice: number;
  halfDayPrice: number;
  allowHalfDays: boolean;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  maxParticipants: number | null;
  participantCount: number;
  isActive: boolean;
  days: CampDayDto[];
}

export interface PublicCampDto {
  id: string;
  name: string;
  description: string | null;
  level: number | null;
  dayPrice: number;
  halfDayPrice: number;
  allowHalfDays: boolean;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  tennisClubName: string;
  maxParticipants: number | null;
  participantCount: number;
  days: PublicCampDayDto[];
}

export interface CreateCampDayTrainerRequest {
  trainerId: string;
  startTime: string;
  endTime: string;
}

export interface CreateCampDayRequest extends CampDayPartsDto {
  /** Gevuld = bestaande dag bijwerken, leeg = nieuwe dag. */
  id?: string;
  date: string;
  trainers: CreateCampDayTrainerRequest[];
}

export interface CreateCampRequest {
  name: string;
  description?: string;
  tennisClubId: string;
  level?: number | null;
  dayPrice: number;
  halfDayPrice: number;
  allowHalfDays: boolean;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  /** Maximum per dagdeel (elke dag apart geteld); null = onbeperkt. */
  maxParticipants?: number | null;
  days: CreateCampDayRequest[];
}

export type UpdateCampRequest = CreateCampRequest & { isActive: boolean };

export interface CampFormFieldDto {
  id: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options: string[] | null;
}

export interface CampEnrollmentFormDto {
  id: string;
  campId: string;
  fields: CampFormFieldDto[];
}

export interface SaveCampFormFieldRequest {
  id?: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options?: string[];
}

export interface CampGroupMemberRequest {
  participantName: string;
  participantEmail: string;
  participantPhone?: string;
  responses: { campFormFieldId: string; value: string }[];
}

export interface SubmitCampEnrollmentRequest {
  participantName: string;
  participantEmail: string;
  participantPhone?: string;
  responses: { campFormFieldId: string; value: string }[];
  enrollmentType?: string; // "solo" | "group"
  groupMembers?: CampGroupMemberRequest[];
  /** Gekozen dagdelen per kampdag; geldt voor de leider en alle groepsleden. */
  days: CampDaySelection[];
}

export interface SubmitCampEnrollmentResult {
  campEnrollmentId: string;
  requiresPayment: boolean;
}

export interface CampPaymentOptions {
  /** Te betalen bedrag: totaal van de inschrijving of de hele groep. */
  price: number;
  onlineAvailable: boolean;
}

export interface CampEnrollmentDto {
  id: string;
  participantName: string;
  participantEmail: string;
  participantPhone: string | null;
  status: string;
  enrolledAt: string;
  groupName: string | null;
  enrollmentGroupId: string | null;
  isGroupLeader: boolean;
  paymentMethod: string | null;
  paymentStatus: string | null;
  /** Bedrag van de eigen (laatste) betaling; bij een online-groep het groepstotaal. */
  paymentAmount: number | null;
  paidAt: string | null;
  /** Mollie-referentie (tr_...) om de betaling in het Mollie-dashboard terug te vinden. */
  molliePaymentId: string | null;
  formResponses: { fieldId: string; fieldLabel: string; value: string }[];
  days: CampDaySelection[];
  totalAmount: number;
}

// ── Admin ──
export async function getCamps(): Promise<CampDto[]> {
  const { data } = await apiClient.get<CampDto[]>("/camps");
  return data;
}
export async function getCampById(id: string): Promise<CampDetailDto> {
  const { data } = await apiClient.get<CampDetailDto>(`/camps/${id}`);
  return data;
}
export async function createCamp(request: CreateCampRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/camps", request);
  return data;
}
export async function updateCamp(id: string, request: UpdateCampRequest): Promise<void> {
  await apiClient.put(`/camps/${id}`, request);
}
export async function deleteCamp(id: string): Promise<void> {
  await apiClient.delete(`/camps/${id}`);
}
export async function getCampForm(campId: string): Promise<CampEnrollmentFormDto | null> {
  const { data } = await publicApiClient.get<CampEnrollmentFormDto | null>(`/public/camps/${campId}/form`);
  return data;
}
export async function saveCampForm(campId: string, fields: SaveCampFormFieldRequest[]): Promise<string> {
  const { data } = await apiClient.put<string>(`/camps/${campId}/form`, { fields });
  return data;
}
export async function getCampEnrollments(campId: string): Promise<CampEnrollmentDto[]> {
  const { data } = await apiClient.get<CampEnrollmentDto[]>(`/camps/${campId}/enrollments`);
  return data;
}
export interface CreateManualCampEnrollmentRequest {
  participantName: string;
  participantEmail: string;
  participantPhone: string | null;
  /** true = cash al ontvangen (bevestigd); false = nog te betalen. Enkel relevant voor betaalde kampen. */
  paid: boolean;
  days: CampDaySelection[];
  responses: { campFormFieldId: string; value: string }[];
}
export async function createManualCampEnrollment(
  campId: string,
  request: CreateManualCampEnrollmentRequest
): Promise<string> {
  const { data } = await apiClient.post<string>(
    `/camps/${campId}/enrollments/manual`,
    request
  );
  return data;
}
export interface AddCampGroupMemberRequest {
  participantName: string;
  participantEmail: string;
  participantPhone: string | null;
  responses: { campFormFieldId: string; value: string }[];
}
export async function addCampGroupMember(
  campId: string,
  groupId: string,
  request: AddCampGroupMemberRequest
): Promise<string> {
  const { data } = await apiClient.post<string>(
    `/camps/${campId}/enrollment-groups/${groupId}/members`,
    request
  );
  return data;
}
export interface UpdateCampEnrollmentRequest {
  participantName: string;
  participantEmail: string;
  participantPhone: string | null;
  /** Volledige set gekozen dagdelen; vervangt de bestaande keuze. */
  days: CampDaySelection[];
  responses: { campFormFieldId: string; value: string }[];
}
export async function updateCampEnrollment(
  campId: string,
  enrollmentId: string,
  request: UpdateCampEnrollmentRequest
): Promise<void> {
  await apiClient.put(`/camps/${campId}/enrollments/${enrollmentId}`, request);
}
export async function cancelCampEnrollment(
  campId: string,
  enrollmentId: string
): Promise<void> {
  await apiClient.delete(`/camps/${campId}/enrollments/${enrollmentId}`);
}
export async function cancelCampEnrollmentGroup(
  campId: string,
  groupId: string
): Promise<void> {
  await apiClient.delete(`/camps/${campId}/enrollment-groups/${groupId}`);
}
export async function exportCamp(campId: string): Promise<Blob> {
  const { data } = await apiClient.get(`/camps/${campId}/export`, {
    responseType: "blob",
  });
  return data;
}
export async function markCampEnrollmentCashPaid(
  campId: string,
  enrollmentId: string
): Promise<void> {
  await apiClient.post(`/camps/${campId}/enrollments/${enrollmentId}/mark-cash-paid`, {});
}

// ── Public ──
export async function getPublicCamp(id: string): Promise<PublicCampDto> {
  const { data } = await publicApiClient.get<PublicCampDto>(`/public/camps/${id}`);
  return data;
}
export async function submitCampEnrollment(
  campId: string, request: SubmitCampEnrollmentRequest
): Promise<SubmitCampEnrollmentResult> {
  const { data } = await publicApiClient.post<SubmitCampEnrollmentResult>(`/public/camps/${campId}/enroll`, request);
  return data;
}
export async function getCampPaymentOptions(
  campId: string, enrollmentId: string
): Promise<CampPaymentOptions> {
  const { data } = await publicApiClient.get<CampPaymentOptions>(
    `/public/camps/${campId}/payment-options`, { params: { enrollmentId } });
  return data;
}
export async function chooseCampPayment(
  campEnrollmentId: string, method: number
): Promise<{ checkoutUrl: string | null }> {
  const { data } = await publicApiClient.post<{ checkoutUrl: string | null }>(
    `/public/camp-enrollments/${campEnrollmentId}/pay`,
    { method }
  );
  return data;
}
