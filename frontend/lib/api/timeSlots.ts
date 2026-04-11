import apiClient from "@/lib/api-client";

export interface TimeSlotDto {
  id: string;
  dayOfWeek: number; // 0 = Monday, 6 = Sunday
  startTime: string; // "HH:mm"
  endTime: string; // "HH:mm"
  courtName: string;
  maxStudents: number;
}

export async function getPublicTimeSlots(
  seriesId: string
): Promise<TimeSlotDto[]> {
  const { data } = await apiClient.get<TimeSlotDto[]>(
    `/public/lessonseries/${seriesId}/timeslots`
  );
  return data;
}
