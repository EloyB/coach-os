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
