import apiClient from "@/lib/api-client";
import type { BillingIntervalName, PlanOption } from "@/lib/billing";

export type SubscriptionStatusName = "Legacy" | "Trialing" | "Active" | "Cancelled" | "Locked";
export type PaymentMethodName = "Mollie" | "BankTransfer";

export interface BillingProfile {
  name: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  invoiceEmail: string;
  vatNumber: string | null;
  /** true/false volgens VIES; null als VIES onbereikbaar was of er geen btw-nummer is. */
  vatVerified: boolean | null;
}

export interface SubscriptionPayment {
  id: string;
  kind: "First" | "Recurring" | "OneOff" | "BankTransfer" | "Deal";
  method: PaymentMethodName;
  status: "Open" | "Paid" | "Failed" | "Canceled" | "Expired" | "Refunded";
  plan: string;
  interval: BillingIntervalName;
  periodStart: string;
  periodEnd: string;
  amountExclVat: number;
  amountInclVat: number;
  paidAt: string | null;
  dueDate: string | null;
  /** Enkel gezet zolang een Mollie-betaling openstaat. */
  checkoutUrl: string | null;
  invoiceStatus: "NotNeeded" | "Pending" | "Created" | "Failed";
  invoiceNumber: string | null;
  invoicePdfUrl: string | null;
  invoiceLastError: string | null;
}

export interface BillingOverview {
  status: SubscriptionStatusName;
  plan: string | null;
  interval: BillingIntervalName | null;
  paymentMethod: PaymentMethodName | null;
  priceExclVat: number;
  priceInclVat: number;
  studentLimit: number | null;
  currentPeriodStart: string | null;
  currentPeriodEnd: string | null;
  cancelAt: string | null;
  trialEndsAt: string | null;
  isDeal: boolean;
  activeStudentCount: number;
  suggestedPlan: string;
  canCheckout: boolean;
  /** false zolang Mollie en Billit niet echt gekoppeld zijn (buiten Development): geen self-service. */
  checkoutAvailable: boolean;
  /** false = fake-Mollie (geen echte betalingen, enkel lokaal/dev). */
  billingConfigured: boolean;
  overageMarginPercent: number;
  overagePricePerStudent: number;
  plans: PlanOption[];
  profile: BillingProfile | null;
  payments: SubscriptionPayment[];
}

export type SaveBillingProfileRequest = Omit<BillingProfile, "vatVerified">;

/** Enumwaarden van de backend: plan 1-3, interval 1=Monthly 2=Yearly, method 1=Mollie 2=BankTransfer. */
export interface CheckoutRequest {
  plan: number;
  interval: 1 | 2;
  method: 1 | 2;
}

export function billingQueryKey(organizationId: string | null | undefined) {
  return ["billing", organizationId ?? null] as const;
}

export async function getBillingOverview(): Promise<BillingOverview> {
  const { data } = await apiClient.get<BillingOverview>("/billing");
  return data;
}

export async function saveBillingProfile(request: SaveBillingProfileRequest): Promise<BillingProfile> {
  const { data } = await apiClient.put<BillingProfile>("/billing/profile", request);
  return data;
}

export async function startCheckout(request: CheckoutRequest): Promise<{ checkoutUrl: string | null }> {
  const { data } = await apiClient.post<{ checkoutUrl: string | null }>("/billing/checkout", request);
  return data;
}

/** Na terugkeer van Mollie: status ophalen (werkt ook zonder webhook). */
export async function syncCheckout(): Promise<BillingOverview> {
  const { data } = await apiClient.post<BillingOverview>("/billing/checkout/sync");
  return data;
}

export async function cancelSubscription(): Promise<BillingOverview> {
  const { data } = await apiClient.post<BillingOverview>("/billing/cancel");
  return data;
}

/** Enkel in Development (fake-Mollie): laat een testbetaling slagen of mislukken. */
export async function setFakePaymentStatus(paymentId: string, status: "paid" | "failed"): Promise<void> {
  await apiClient.post(`/dev/billing/fake-payments/${encodeURIComponent(paymentId)}/status`, { status });
}
