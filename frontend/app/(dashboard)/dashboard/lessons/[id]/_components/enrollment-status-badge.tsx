import { Badge } from "@/components/ui/badge";
import { enrollmentDisplayStatus } from "@/lib/status-styles";

/**
 * Statusbadge voor een inschrijving. Toont "Betaling mislukt" wanneer de laatste
 * betaling faalde, anders de gewone inschrijvingsstatus.
 */
export function EnrollmentStatusBadge({
  status,
  paymentStatus,
  className = "",
}: {
  status: string;
  paymentStatus: string | null | undefined;
  className?: string;
}) {
  const style = enrollmentDisplayStatus(status, paymentStatus);
  if (!style) return null;
  return (
    <Badge className={`${style.className} border-0 ${className}`}>
      {style.label}
    </Badge>
  );
}
