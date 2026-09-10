import apiClient from "@/lib/api-client";

export interface MollieConnectionStatusDto {
  connected: boolean;
  mollieOrganizationName: string | null;
  connectedAt: string | null;
}

/**
 * Eén gedeelde query key voor de Mollie-status. Instellingen, de lessenreeks-wizard en
 * lessenreeks-bewerken lezen dezelfde status; met één key raakt een invalidate na
 * (ont)koppelen ook de "online betalen"-checkbox in de andere schermen.
 */
export const MOLLIE_CONNECTION_QUERY_KEY = ["mollieConnection"] as const;

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
