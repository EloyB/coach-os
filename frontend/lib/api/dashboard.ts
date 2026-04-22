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
