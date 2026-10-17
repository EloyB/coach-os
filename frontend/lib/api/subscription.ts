import apiClient from "@/lib/api-client";

export type SubscriptionStatus = "Legacy" | "Trialing" | "Active";
export type SubscriptionAccessLevel = "Full" | "ReadOnly";

export interface SubscriptionStatusDto {
  status: SubscriptionStatus;
  accessLevel: SubscriptionAccessLevel;
  /** Laatste dag van de proef (yyyy-MM-dd); null buiten een proef. */
  trialEndsAt: string | null;
  trialDaysRemaining: number | null;
  isTrialExpired: boolean;
}

export async function getSubscriptionStatus(): Promise<SubscriptionStatusDto> {
  // Puur informatief voor de banner: lukt dit niet, dan tonen we gewoon niets.
  const { data } = await apiClient.get<SubscriptionStatusDto>("/subscription/status", {
    skipErrorToast: true,
  });
  return data;
}
