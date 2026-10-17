import apiClient from "@/lib/api-client";

export interface UpcomingLessonDto {
  id: string;
  seriesName: string;
  date: string;
  startTime: string;
  endTime: string;
  courtName: string;
  trainerName: string;
}

export interface DashboardSummaryDto {
  activeSeriesCount: number;
  lessonsTodayCount: number;
  lessonsThisWeekCount: number;
  totalEnrollmentCount: number;
  activeTrainerCount: number;
  tennisClubCount: number;
  upcomingLessons: UpcomingLessonDto[];
}

export async function getDashboardSummary(): Promise<DashboardSummaryDto> {
  const { data } = await apiClient.get<DashboardSummaryDto>("/dashboard");
  return data;
}

// ─── Inbox ───────────────────────────────────────────────────────────────────

export interface InboxItemDto {
  type: string;
  refType: string;
  refId: string;
  title: string;
  body: string;
  meta: string;
  severity: string;
  createdAt: string;
}

export interface InboxResponse {
  items: InboxItemDto[];
  updatedAt: string;
}

export async function getDashboardInbox(limit: number = 10): Promise<InboxResponse> {
  const { data } = await apiClient.get<InboxResponse>("/dashboard/inbox", {
    params: { limit },
  });
  return data;
}

// ─── Metrics ─────────────────────────────────────────────────────────────────

export interface WeekMetricDto {
  week: string;
  value: number;
}

export interface DashboardMetricsDto {
  lessons: WeekMetricDto[];
  occupancyPct: WeekMetricDto[];
  outstandingCents: WeekMetricDto[];
}

export async function getDashboardMetrics(weeks: number = 7): Promise<DashboardMetricsDto> {
  const { data } = await apiClient.get<DashboardMetricsDto>("/dashboard/metrics", {
    params: { weeks },
  });
  return data;
}

// ─── Eigen lessen (trainersdashboard) ────────────────────────────────────────

export interface MyLessonDto {
  id: string;
  /** Null voor losse lessen. */
  seriesId: string | null;
  seriesName: string | null;
  date: string;
  startTime: string;
  endTime: string;
  courtName: string | null;
  tennisClubName: string | null;
  /** Aantal actieve inschrijvingen van de reeks; null voor losse lessen. */
  enrolledCount: number | null;
}

export async function getMyLessons(from: string, to: string): Promise<MyLessonDto[]> {
  const { data } = await apiClient.get<MyLessonDto[]>("/dashboard/my-lessons", {
    params: { from, to },
  });
  return data;
}

export type MyLessonParticipantStatus =
  | "confirmed"
  | "awaiting"
  | "proposed"
  | "accepted"
  | "pending"
  | "declined";

export interface MyLessonParticipantDto {
  name: string;
  status: MyLessonParticipantStatus;
}

export interface MyLessonDetailDto {
  id: string;
  seriesId: string | null;
  seriesName: string | null;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  courtName: string | null;
  tennisClubName: string | null;
  tennisClubAddress: string | null;
  /** 1 = Beginner, 2 = Gemiddeld, 3 = Gevorderd. */
  level: number | null;
  notes: string | null;
  maxParticipants: number;
  participants: MyLessonParticipantDto[];
}

export async function getMyLesson(lessonId: string): Promise<MyLessonDetailDto> {
  const { data } = await apiClient.get<MyLessonDetailDto>(
    `/dashboard/my-lessons/${lessonId}`,
  );
  return data;
}

// ─── Overzicht + kampdagen (trainersdashboard) ───────────────────────────────

export interface MyCampDayDto {
  campId: string;
  campName: string;
  date: string;
  /** Het venster van de trainer zelf (kan korter zijn dan de kampuren). */
  startTime: string;
  endTime: string;
  tennisClubName: string | null;
}

export async function getMyCampDays(from: string, to: string): Promise<MyCampDayDto[]> {
  const { data } = await apiClient.get<MyCampDayDto[]>("/dashboard/my-camp-days", {
    params: { from, to },
  });
  return data;
}

export interface MyOverviewDto {
  /** Eerstvolgende les die nog niet voorbij is; null als er niets gepland staat. */
  nextLesson: MyLessonDto | null;
  /** Geplande uren (lessen + kampdagen) in de huidige week (ma–zo). */
  hoursThisWeek: number;
  /** Geplande uren (lessen + kampdagen) in de huidige kalendermaand. */
  hoursThisMonth: number;
  /** Wekelijkse capaciteit in uren; 0 = niet ingesteld. */
  weeklyCapacityHours: number;
}

export async function getMyOverview(): Promise<MyOverviewDto> {
  const { data } = await apiClient.get<MyOverviewDto>("/dashboard/my-overview");
  return data;
}
