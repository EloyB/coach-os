import apiClient from "@/lib/api-client";

export type PaymentStatus = "Pending" | "Paid" | "Failed" | "Refunded";

export interface PaymentStatusDto {
  paymentId: string;
  status: PaymentStatus;
  amount: number;
  currency: string;
  checkoutUrl: string | null;
  paidAt: string | null;
  failureReason: string | null;
}

/**
 * Status van de meest recente betaling voor een inschrijving. <code>sync=true</code>
 * dwingt een Mollie-roundtrip af — gebruikt door de thank-you page polling
 * zodat lokale dev (geen webhook delivery naar localhost) toch live status
 * updates krijgt.
 */
export async function getPaymentStatusByEnrollment(
  enrollmentId: string,
  sync = false,
): Promise<PaymentStatusDto> {
  const { data } = await apiClient.get<PaymentStatusDto>(
    `/public/payments/by-enrollment/${enrollmentId}`,
    { params: { sync } },
  );
  return data;
}
