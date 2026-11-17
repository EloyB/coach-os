import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui/badge";
import { paymentMethodDisplay } from "@/lib/payment-method";

/** Toont hoe de deelnemer koos te betalen; niets zolang er nog geen keuze is. */
export function PaymentMethodBadge({
  method,
}: {
  method: string | null | undefined;
}) {
  const t = useTranslations("enrollmentsTable");
  const display = paymentMethodDisplay(method);
  if (!display) return null;
  return (
    <Badge className={`${display.className} border-0 text-xs`}>
      {t(display.labelKey)}
    </Badge>
  );
}
