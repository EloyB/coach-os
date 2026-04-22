"use client";

import { use, useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import {
  CheckCircle2,
  Clock,
  MapPin,
  CalendarDays,
  Euro,
  Users,
  AlertTriangle,
  ArrowRight,
} from "lucide-react";
import {
  getAssignmentDetails,
  confirmAssignment,
  declineAssignment,
  pickAlternativeSlot,
} from "@/lib/api/confirmation";
import type {
  AssignmentDetailsDto,
  AvailableSlotDto,
} from "@/lib/api/confirmation";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";
import { Spinner } from "@/components/ui/spinner";

// dayOfWeek: 0=Sunday..6=Saturday
const DAY_NAMES = [
  "Zondag",
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
];

export default function ConfirmationPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = use(params);
  const t = useTranslations("confirmation");

  const [details, setDetails] = useState<AssignmentDetailsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorState, setErrorState] = useState<"expired" | "not-found" | null>(null);

  // Flow state
  const [step, setStep] = useState<"view" | "confirm" | "decline" | "success" | "moved">("view");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [availableSlots, setAvailableSlots] = useState<AvailableSlotDto[]>([]);
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);
  const [movedSlotLabel, setMovedSlotLabel] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const data = await getAssignmentDetails(token);
        setDetails(data);
        if (data.status === "Confirmed") setStep("success");
      } catch (err: unknown) {
        const response = (err as { response?: { status?: number; data?: { message?: string } } })?.response;
        if (response?.status === 400) {
          setErrorState("expired");
        } else {
          setErrorState("not-found");
        }
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [token]);

  async function handleConfirm() {
    setSubmitting(true);
    setSubmitError(null);
    try {
      const result = await confirmAssignment(
        token,
        2, // Cash
        window.location.origin + "/confirmation/done"
      );
      if (result.isConfirmed) {
        setStep("success");
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSubmitError(msg ?? "Er ging iets mis.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDecline() {
    setSubmitting(true);
    setSubmitError(null);
    try {
      const result = await declineAssignment(token);
      setAvailableSlots(result.availableSlots);
      setStep("decline");
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSubmitError(msg ?? "Er ging iets mis.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handlePickAlternative() {
    if (!selectedSlotId) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      const slot = availableSlots.find((s) => s.weeklyTemplateEntryId === selectedSlotId);
      const result = await pickAlternativeSlot(
        token,
        selectedSlotId,
        2, // Cash
        window.location.origin + "/confirmation/done"
      );
      if (result.isConfirmed) {
        setMovedSlotLabel(
          slot ? `${DAY_NAMES[slot.dayOfWeek]} ${slot.startTime}` : ""
        );
        setStep("moved");
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSubmitError(msg ?? "Er ging iets mis.");
    } finally {
      setSubmitting(false);
    }
  }

  // ─── Loading / Error states ─────────────────────────────────────────────

  if (loading) {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16">
          <Spinner className="h-5 w-5" />
          <p className="text-sm text-gray-500 mt-3">{t("loading")}</p>
        </div>
      </Shell>
    );
  }

  if (errorState === "expired") {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center">
          <div className="w-14 h-14 rounded-full bg-amber-100 flex items-center justify-center mb-4">
            <AlertTriangle className="w-7 h-7 text-amber-600" />
          </div>
          <h2 className="text-lg font-bold text-gray-900 mb-2">{t("expired")}</h2>
          <p className="text-sm text-gray-500">{t("expiredDesc")}</p>
        </div>
      </Shell>
    );
  }

  if (errorState === "not-found" || !details) {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center">
          <div className="w-14 h-14 rounded-full bg-red-100 flex items-center justify-center mb-4">
            <AlertTriangle className="w-7 h-7 text-red-600" />
          </div>
          <h2 className="text-lg font-bold text-gray-900 mb-2">{t("notFound")}</h2>
          <p className="text-sm text-gray-500">{t("notFoundDesc")}</p>
        </div>
      </Shell>
    );
  }

  // ─── Success states ─────────────────────────────────────────────────────

  if (step === "success") {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center">
          <div className="w-14 h-14 rounded-full bg-green-100 flex items-center justify-center mb-4">
            <CheckCircle2 className="w-7 h-7 text-green-600" />
          </div>
          <h2 className="text-lg font-bold text-gray-900 mb-2">{t("successTitle")}</h2>
          <p className="text-sm text-gray-500">{t("successDesc")}</p>
        </div>
      </Shell>
    );
  }

  if (step === "moved") {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center">
          <div className="w-14 h-14 rounded-full bg-green-100 flex items-center justify-center mb-4">
            <CheckCircle2 className="w-7 h-7 text-green-600" />
          </div>
          <h2 className="text-lg font-bold text-gray-900 mb-2">{t("successMoved")}</h2>
          <p className="text-sm text-gray-500">
            Je plek is verplaatst naar {movedSlotLabel}.
          </p>
        </div>
      </Shell>
    );
  }

  // Format expiry
  const expiryDate = new Date(details.expiresAt);
  const expiryFormatted = `${expiryDate.toLocaleDateString("nl-BE", { day: "2-digit", month: "2-digit" })} om ${expiryDate.toLocaleTimeString("nl-BE", { hour: "2-digit", minute: "2-digit" })}`;

  // ─── Decline: pick alternative ──────────────────────────────────────────

  if (step === "decline") {
    return (
      <Shell>
        <div className="p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-1">
            {t("chooseAlternative")}
          </h2>
          <p className="text-sm text-gray-500 mb-5">{t("chooseAlternativeDesc")}</p>

          {submitError && (
            <div className="mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              <p className="text-red-600 text-sm">{submitError}</p>
            </div>
          )}

          <div className="space-y-2 mb-5">
            {availableSlots.map((slot) => (
              <label
                key={slot.weeklyTemplateEntryId}
                className={`flex items-center justify-between p-3 rounded-lg border-2 cursor-pointer transition ${
                  selectedSlotId === slot.weeklyTemplateEntryId
                    ? "border-tennis-green bg-tennis-green/5"
                    : "border-gray-200 hover:border-tennis-green/30"
                }`}
              >
                <input
                  type="radio"
                  name="alternative"
                  value={slot.weeklyTemplateEntryId}
                  checked={selectedSlotId === slot.weeklyTemplateEntryId}
                  onChange={() => setSelectedSlotId(slot.weeklyTemplateEntryId)}
                  className="sr-only"
                />
                <div>
                  <div className="text-sm font-medium text-gray-900">
                    {DAY_NAMES[slot.dayOfWeek]} {slot.startTime} — {slot.endTime}
                  </div>
                  {slot.courtName && (
                    <div className="text-xs text-gray-500">{slot.courtName}</div>
                  )}
                </div>
                <span className="text-xs text-gray-400">
                  {slot.remainingCapacity} {t("spotsLeft")}
                </span>
              </label>
            ))}
          </div>

          <button
            type="button"
            disabled={!selectedSlotId || submitting}
            onClick={handlePickAlternative}
            className="w-full bg-tennis-green text-white py-3 rounded-lg font-medium hover:bg-tennis-green/90 transition text-sm disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {submitting ? <Spinner className="h-5 w-5" /> : <ArrowRight size={16} />}
            {submitting ? t("picking") : t("pickSlot")}
          </button>
        </div>
      </Shell>
    );
  }

  // ─── Main view: confirm or decline ──────────────────────────────────────

  return (
    <Shell>
      <div className="p-6">
        {/* Series name */}
        <h2 className="text-lg font-semibold text-gray-900 mb-4">
          {details.seriesName}
        </h2>

        {/* Slot details */}
        <div className="bg-[#FAFAF8] rounded-lg p-4 mb-4 space-y-2.5">
          <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
            {t("yourSlot")}
          </h3>
          <div className="flex items-center gap-2 text-sm text-gray-700">
            <CalendarDays size={15} className="text-tennis-green shrink-0" />
            {DAY_NAMES[details.dayOfWeek]} {details.startTime} — {details.endTime}
          </div>
          {details.courtName && (
            <div className="flex items-center gap-2 text-sm text-gray-700">
              <MapPin size={15} className="text-tennis-green shrink-0" />
              {details.courtName}
            </div>
          )}
          <div className="flex items-center gap-2 text-sm text-gray-700">
            <Euro size={15} className="text-tennis-green shrink-0" />
            €{details.pricePerPerson.toFixed(2)} {t("perPerson")}
            {details.isGroup && (
              <span className="text-gray-400">
                — {t("total")}: €{details.totalPrice.toFixed(2)}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2 text-xs text-gray-400">
            <Clock size={13} className="shrink-0" />
            {t("expiresAt", {
              date: expiryDate.toLocaleDateString("nl-BE", { day: "2-digit", month: "2-digit" }),
              time: expiryDate.toLocaleTimeString("nl-BE", { hour: "2-digit", minute: "2-digit" }),
            })}
          </div>
        </div>

        {/* Group info */}
        {details.isGroup && details.groupMemberNames.length > 0 && (
          <div className="bg-blue-50 border border-blue-100 rounded-lg p-3 mb-4">
            <div className="flex items-center gap-2 mb-1">
              <Users size={14} className="text-blue-600" />
              <span className="text-xs font-medium text-blue-800">
                Groepsinschrijving
              </span>
            </div>
            <p className="text-xs text-blue-700">
              Je bevestigt voor {details.studentName} + {details.groupMemberNames.length} leden ({details.groupMemberNames.join(", ")}). Totaal: €{details.totalPrice.toFixed(2)}.
            </p>
          </div>
        )}

        {submitError && (
          <div className="mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
            <p className="text-red-600 text-sm">{submitError}</p>
          </div>
        )}

        {/* Confirm step: payment method */}
        {step === "confirm" && (
          <div className="mb-5">
            <h3 className="text-sm font-medium text-gray-700 mb-2">
              {t("paymentMethod")}
            </h3>
            <div className="space-y-2">
              <label className="flex items-center gap-3 p-3 rounded-lg border-2 border-tennis-green bg-tennis-green/5 cursor-pointer">
                <input type="radio" name="payment" value="cash" defaultChecked className="accent-tennis-green" />
                <span className="text-sm text-gray-900">{t("cash")}</span>
              </label>
              <label className="flex items-center gap-3 p-3 rounded-lg border-2 border-gray-200 opacity-50 cursor-not-allowed">
                <input type="radio" name="payment" value="online" disabled className="accent-tennis-green" />
                <div>
                  <span className="text-sm text-gray-500">{t("online")}</span>
                  <span className="text-xs text-gray-400 ml-2 italic">({t("onlineComingSoon")})</span>
                </div>
              </label>
            </div>
          </div>
        )}

        {/* Action buttons */}
        {step === "view" && (
          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => setStep("confirm")}
              className="flex-1 bg-tennis-green text-white py-3 rounded-lg font-medium hover:bg-tennis-green/90 transition text-sm"
            >
              {t("confirm")}
            </button>
            <button
              type="button"
              onClick={handleDecline}
              disabled={submitting}
              className="flex-1 border border-gray-300 text-gray-700 py-3 rounded-lg font-medium hover:bg-gray-50 transition text-sm disabled:opacity-50"
            >
              {submitting ? <Spinner className="h-5 w-5" /> : t("decline")}
            </button>
          </div>
        )}

        {step === "confirm" && (
          <button
            type="button"
            onClick={handleConfirm}
            disabled={submitting}
            className="w-full bg-tennis-green text-white py-3 rounded-lg font-medium hover:bg-tennis-green/90 transition text-sm disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {submitting && <Spinner className="h-5 w-5" />}
            {submitting ? t("confirming") : t("confirmSlot")}
          </button>
        )}
      </div>
    </Shell>
  );
}

// ─── Shell wrapper ────────────────────────────────────────────────────────

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-[#FAFAF8]">
      <div className="max-w-lg mx-auto px-4 py-8">
        {/* Logo */}
        <div className="flex items-center gap-2 mb-8">
          <div className="w-8 h-8 bg-tennis-green rounded-full flex items-center justify-center">
            <TennisBallIcon className="w-4 h-4 text-white" strokeWidth={2} />
          </div>
          <span className="font-semibold text-lg text-tennis-green">CoachOS</span>
        </div>

        {/* Card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          {children}
        </div>

        {/* Footer */}
        <div className="text-center mt-8 text-xs text-gray-400">
          Powered by CoachOS
        </div>
      </div>
    </div>
  );
}
