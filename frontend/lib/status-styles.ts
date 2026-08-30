/**
 * Tailwind class + display label lookups for status enums rendered as badges.
 * Keyed by the backend status string exactly as it appears on the DTO.
 */

export const assignmentStatusStyles: Record<string, string> = {
  Confirmed: "bg-tennis-lime/20 text-tennis-green",
  AwaitingConfirmation: "bg-amber-100 text-amber-800",
  Proposed: "bg-blue-100 text-blue-800",
};

export const assignmentStatusFallback = "bg-gray-100 text-gray-700";

export interface EnrollmentStatusStyle {
  className: string;
  label: string;
}

export const enrollmentStatusStyles: Record<string, EnrollmentStatusStyle> = {
  Confirmed: { className: "bg-green-100 text-green-700", label: "Bevestigd" },
  Pending: { className: "bg-amber-100 text-amber-700", label: "In afwachting" },
  Cancelled: { className: "bg-red-100 text-red-700", label: "Geannuleerd" },
  Waitlisted: { className: "bg-blue-100 text-blue-700", label: "Wachtlijst" },
  PendingPayment: {
    className: "bg-orange-100 text-orange-700",
    label: "Betaling in afwachting",
  },
};
