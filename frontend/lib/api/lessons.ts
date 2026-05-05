import apiClient from "@/lib/api-client";

export interface RescheduleLessonRequest {
  newDate: string; // yyyy-MM-dd
  newStartTime: string; // HH:mm
  newEndTime: string; // HH:mm
  reason?: string;
}

export interface RescheduleLessonResultDto {
  newLessonId: string;
  notifiedCount: number;
}

export async function rescheduleLesson(
  lessonId: string,
  request: RescheduleLessonRequest
): Promise<RescheduleLessonResultDto> {
  const { data } = await apiClient.post<RescheduleLessonResultDto>(
    `/lessons/${lessonId}/reschedule`,
    request
  );
  return data;
}
