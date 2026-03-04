import apiClient from "@/lib/api-client";

export interface TennisClubDto {
  id: string;
  name: string;
  address: string;
}

export interface CreateTennisClubRequest {
  name: string;
  address: string;
}

export async function getTennisClubs(): Promise<TennisClubDto[]> {
  const { data } = await apiClient.get<TennisClubDto[]>("/tennisclubs");
  return data;
}

export async function createTennisClub(request: CreateTennisClubRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/tennisclubs", request);
  return data;
}

export async function deleteTennisClub(id: string): Promise<void> {
  await apiClient.delete(`/tennisclubs/${id}`);
}
