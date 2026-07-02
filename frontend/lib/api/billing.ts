import apiClient from "@/lib/api-client";

export interface BillingStatusDto {
  status: string;
  trialEndsAt: string | null;
  trialDaysLeft: number | null;
  hasAccess: boolean;
}

export async function getBillingStatus(): Promise<BillingStatusDto> {
  const { data } = await apiClient.get<BillingStatusDto>("/billing/status");
  return data;
}
