import apiClient from "@/lib/api-client";

export type SubscriptionStatus = "Legacy" | "Trialing" | "Active" | "Cancelled" | "Locked";
export type SubscriptionAccessLevel = "Full" | "ReadOnly";

export interface SubscriptionStatusDto {
  status: SubscriptionStatus;
  accessLevel: SubscriptionAccessLevel;
  /** Laatste dag van de proef (yyyy-MM-dd); null buiten een proef. */
  trialEndsAt: string | null;
  trialDaysRemaining: number | null;
  isTrialExpired: boolean;
  /** Laatste dag van de lopende betaalde periode; null zonder betaald abonnement. */
  currentPeriodEnd: string | null;
  /** Opgezegd per deze datum; null als niet opgezegd. */
  cancelAt: string | null;
}

export async function getSubscriptionStatus(): Promise<SubscriptionStatusDto> {
  // Puur informatief voor de banner: lukt dit niet, dan tonen we gewoon niets.
  const { data } = await apiClient.get<SubscriptionStatusDto>("/subscription/status", {
    skipErrorToast: true,
  });
  return data;
}
