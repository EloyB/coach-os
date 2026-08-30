import apiClient from "@/lib/api-client";

export interface StudentLesson {
  assignmentId: string;
  seriesId: string;
  seriesName: string;
  seriesStartDate: string;
  seriesEndDate: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courtName: string | null;
  trainerName: string | null;
  status: string;
  participantName: string;
  isGroup: boolean;
  groupSize: number;
  price: number;
  paymentStatus: string | null;
}

export async function getMyLessons(): Promise<StudentLesson[]> {
  const { data } = await apiClient.get<StudentLesson[]>("/student/lessons");
  return data;
}

export async function getMyLesson(id: string): Promise<StudentLesson> {
  const { data } = await apiClient.get<StudentLesson>(`/student/lessons/${id}`);
  return data;
}
