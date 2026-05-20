import apiClient from "@/lib/api-client";

export interface MollieConnectionStatusDto {
  connected: boolean;
  mollieOrganizationName: string | null;
  connectedAt: string | null;
}

export interface StartConnectResponse {
  authorizationUrl: string;
}

export async function getMollieConnectionStatus(): Promise<MollieConnectionStatusDto> {
  const { data } = await apiClient.get<MollieConnectionStatusDto>(
    "/mollie-connect/status",
  );
  return data;
}

export async function startMollieConnect(): Promise<StartConnectResponse> {
  const { data } = await apiClient.post<StartConnectResponse>(
    "/mollie-connect/start",
  );
  return data;
}

export async function disconnectMollie(): Promise<void> {
  await apiClient.post("/mollie-connect/disconnect");
}
