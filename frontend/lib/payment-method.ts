/** Vertaalsleutel + Tailwind-klassen voor de betaalmethode op een inschrijving. */
export interface PaymentMethodDisplay {
  labelKey: "paymentMethodOnline" | "paymentMethodCash" | "paymentMethodTransfer";
  className: string;
}

const neutral = "bg-gray-100 text-gray-600";

const paymentMethodDisplays: Record<string, PaymentMethodDisplay> = {
  Online: { labelKey: "paymentMethodOnline", className: neutral },
  // Valt bewust op: deze betaling volgt de club zelf op, dus die rijen moeten herkenbaar zijn.
  Cash: { labelKey: "paymentMethodCash", className: "bg-violet-100 text-violet-700" },
  Transfer: { labelKey: "paymentMethodTransfer", className: neutral },
};

/** Weergave voor de betaalmethode van de DTO; `undefined` zolang er geen keuze is. */
export function paymentMethodDisplay(
  method: string | null | undefined,
): PaymentMethodDisplay | undefined {
  return method ? paymentMethodDisplays[method] : undefined;
}
