"use client";

import { use, useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import {
  CheckCircle2,
  Clock,
  AlertTriangle,
  ArrowRight,
  Calendar,
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
import { LogoMark } from "@/components/ui/logo-mark";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import { InkHeroCard } from "@/components/ui/ink-hero-card";

const DAY_NAMES = [
  "Zondag", "Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag",
];
const DAY_SHORT = ["Zo", "Ma", "Di", "Wo", "Do", "Vr", "Za"];

function Spinner() {
  return (
    <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
  );
}

function useCountdown(expiresAt: string) {
  const [remaining, setRemaining] = useState("");
  useEffect(() => {
    function calc() {
      const diff = new Date(expiresAt).getTime() - Date.now();
      if (diff <= 0) { setRemaining("0 min"); return; }
      const h = Math.floor(diff / 3600000);
      const m = Math.floor((diff % 3600000) / 60000);
      setRemaining(h > 0 ? `${h} u ${m} min` : `${m} min`);
    }
    calc();
    const id = setInterval(calc, 60000);
    return () => clearInterval(id);
  }, [expiresAt]);
  return remaining;
}

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
  const [step, setStep] = useState<"view" | "confirm" | "decline" | "success" | "moved">("view");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [availableSlots, setAvailableSlots] = useState<AvailableSlotDto[]>([]);
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);
  const [movedSlotLabel, setMovedSlotLabel] = useState("");
  // 1 = Online (Mollie), 2 = Cash. Default Cash zodat clubs zonder Mollie-
  // koppeling de bestaande flow blijven krijgen.
  const [paymentMethod, setPaymentMethod] = useState<1 | 2>(2);

  useEffect(() => {
    async function load() {
      try {
        const data = await getAssignmentDetails(token);
        setDetails(data);
        if (data.status === "Confirmed") setStep("success");
      } catch (err: unknown) {
        const response = (err as { response?: { status?: number } })?.response;
        setErrorState(response?.status === 400 ? "expired" : "not-found");
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
      const result = await confirmAssignment(token, paymentMethod, window.location.origin + "/confirmation/done");
      // Online → BE creëerde een Mollie payment; redirect naar de checkout URL.
      if (result.checkoutUrl) {
        window.location.href = result.checkoutUrl;
        return;
      }
      if (result.isConfirmed) setStep("success");
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
      const result = await pickAlternativeSlot(token, selectedSlotId, paymentMethod, window.location.origin + "/confirmation/done");
      if (result.checkoutUrl) {
        window.location.href = result.checkoutUrl;
        return;
      }
      if (result.isConfirmed) {
        setMovedSlotLabel(slot ? `${DAY_NAMES[slot.dayOfWeek]} ${slot.startTime}` : "");
        setStep("moved");
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSubmitError(msg ?? "Er ging iets mis.");
    } finally {
      setSubmitting(false);
    }
  }

  // ─── Loading / Error ──────────────────────────────────────────────────
  if (loading) {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16">
          <Spinner />
          <p className="text-sm text-ink-3 mt-3">{t("loading")}</p>
        </div>
      </Shell>
    );
  }

  if (errorState === "expired") {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center px-6">
          <div className="w-14 h-14 rounded-full bg-amber-100 flex items-center justify-center mb-4">
            <AlertTriangle className="w-7 h-7 text-amber-600" />
          </div>
          <h2 className="text-lg font-bold text-ink mb-2">{t("expired")}</h2>
          <p className="text-sm text-ink-3">{t("expiredDesc")}</p>
        </div>
      </Shell>
    );
  }

  if (errorState === "not-found" || !details) {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center px-6">
          <div className="w-14 h-14 rounded-full bg-red-100 flex items-center justify-center mb-4">
            <AlertTriangle className="w-7 h-7 text-red-600" />
          </div>
          <h2 className="text-lg font-bold text-ink mb-2">{t("notFound")}</h2>
          <p className="text-sm text-ink-3">{t("notFoundDesc")}</p>
        </div>
      </Shell>
    );
  }

  // ─── Success ──────────────────────────────────────────────────────────
  if (step === "success") {
    const calendarUrl = `${process.env.NEXT_PUBLIC_API_URL?.replace(/\/api$/, "")}/confirm/${token}/calendar.ics`;

    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center px-6">
          <div className="w-14 h-14 rounded-full bg-green-100 flex items-center justify-center mb-4">
            <CheckCircle2 className="w-7 h-7 text-green-600" />
          </div>
          <h2 className="text-lg font-bold text-ink mb-2">{t("successTitle")}</h2>
          <p className="text-sm text-ink-3 mb-5">{t("successDesc")}</p>
          <a
            href={calendarUrl}
            download
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-ink text-white text-xs font-semibold rounded-lg hover:bg-ink/90 transition-colors"
          >
            <Calendar size={14} />
            {t("addToCalendar")}
          </a>
        </div>
      </Shell>
    );
  }

  if (step === "moved") {
    return (
      <Shell>
        <div className="flex flex-col items-center py-16 text-center px-6">
          <div className="w-14 h-14 rounded-full bg-green-100 flex items-center justify-center mb-4">
            <CheckCircle2 className="w-7 h-7 text-green-600" />
          </div>
          <h2 className="text-lg font-bold text-ink mb-2">{t("successMoved")}</h2>
          <p className="text-sm text-ink-3">Je plek is verplaatst naar {movedSlotLabel}.</p>
        </div>
      </Shell>
    );
  }

  // ─── Decline: pick alternative ────────────────────────────────────────
  if (step === "decline") {
    return (
      <Shell>
        <div className="p-6">
          <h2 className="text-lg font-bold text-ink mb-1">{t("chooseAlternative")}</h2>
          <p className="text-sm text-ink-3 mb-5">{t("chooseAlternativeDesc")}</p>

          {submitError && (
            <div className="mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              <p className="text-red-600 text-sm">{submitError}</p>
            </div>
          )}

          <div className="space-y-2 mb-5">
            {availableSlots.map((slot) => (
              <label
                key={slot.weeklyTemplateEntryId}
                className={`flex items-center justify-between p-3.5 rounded-[10px] border-2 cursor-pointer transition ${
                  selectedSlotId === slot.weeklyTemplateEntryId
                    ? "border-tennis-green bg-tennis-green/[.04]"
                    : "border-rule hover:border-tennis-green/30"
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
                  <div className="text-[12.5px] font-semibold text-ink">
                    {DAY_NAMES[slot.dayOfWeek]} {slot.startTime} — {slot.endTime}
                  </div>
                  {slot.courtName && (
                    <div className="text-[11px] text-ink-3">{slot.courtName}</div>
                  )}
                </div>
                <Mono className="text-[10.5px] text-ink-3">
                  {slot.remainingCapacity} {t("spotsLeft")}
                </Mono>
              </label>
            ))}
          </div>

          <button
            type="button"
            disabled={!selectedSlotId || submitting}
            onClick={handlePickAlternative}
            className="w-full bg-ink text-white py-3 rounded-[10px] font-semibold text-[13px] disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {submitting ? <Spinner /> : <ArrowRight size={16} />}
            {submitting ? t("picking") : t("pickSlot")}
          </button>
        </div>
      </Shell>
    );
  }

  // ─── Main view ────────────────────────────────────────────────────────
  return (
    <Shell>
      <ConfirmCountdown expiresAt={details.expiresAt} />

      <div className="bg-white border border-rule rounded-[14px] overflow-hidden">
        {/* Greeting */}
        <div className="px-6 pt-5 pb-3.5">
          <Mono className="text-[11px] text-ink-3">Hi {details.studentName}!</Mono>
          <h2 className="text-xl font-extrabold text-ink tracking-tight mt-1 mb-0.5">
            Je hebt een plek
          </h2>
          <p className="text-[12.5px] text-ink-3">
            Bevestig om je inschrijving vast te leggen.
          </p>
        </div>

        {/* Ink slot card */}
        <div className="mx-6 mb-4">
          <InkHeroCard className="p-5">
            <div className="flex items-start justify-between mb-3">
              <div>
                <Mono className="text-[10.5px] text-tennis-lime uppercase tracking-[0.1em]">
                  {DAY_SHORT[details.dayOfWeek]}
                </Mono>
                <Mono className="text-[28px] font-extrabold mt-1 block">
                  {details.startTime} <span className="text-[#8a8377] text-lg">— {details.endTime}</span>
                </Mono>
              </div>
              <div className="text-right">
                <Mono className="text-[10.5px] text-[#8a8377]">{t("total")}</Mono>
                <Mono className="text-lg font-extrabold block">€{details.totalPrice.toFixed(2)}</Mono>
              </div>
            </div>
            <div className="text-[12.5px] text-[#e9e4db] space-y-1">
              <div>{details.seriesName}</div>
              {details.courtName && (
                <div className="text-[#a8a195]">{details.courtName}</div>
              )}
            </div>
          </InkHeroCard>
        </div>

        {/* Payment method */}
        {step === "confirm" && (
          <div className="px-6 pb-[18px]">
            <SlashLabel className="mb-2.5">/betaal</SlashLabel>
            <div className="space-y-2">
              <button
                type="button"
                onClick={() => setPaymentMethod(2)}
                className={`w-full flex items-center gap-3 p-3.5 border-2 rounded-[10px] text-left transition-colors ${
                  paymentMethod === 2
                    ? "border-tennis-green bg-tennis-green/[.04]"
                    : "border-rule bg-white hover:border-gray-300"
                }`}
              >
                <div
                  className={`w-4 h-4 rounded-full border-[5px] bg-white ${
                    paymentMethod === 2 ? "border-tennis-green" : "border-gray-300"
                  }`}
                />
                <div className="flex-1">
                  <div className="text-[13px] font-semibold text-ink">{t("cash")}</div>
                </div>
              </button>
              <button
                type="button"
                onClick={() => setPaymentMethod(1)}
                className={`w-full flex items-center gap-3 p-3.5 border-2 rounded-[10px] text-left transition-colors ${
                  paymentMethod === 1
                    ? "border-tennis-green bg-tennis-green/[.04]"
                    : "border-rule bg-white hover:border-gray-300"
                }`}
              >
                <div
                  className={`w-4 h-4 rounded-full border-[5px] bg-white ${
                    paymentMethod === 1 ? "border-tennis-green" : "border-gray-300"
                  }`}
                />
                <div className="flex-1">
                  <div className="text-[13px] font-semibold text-ink">{t("online")}</div>
                  <div className="text-[11px] text-gray-500 mt-0.5">{t("onlineHelp")}</div>
                </div>
              </button>
            </div>
          </div>
        )}

        {submitError && (
          <div className="mx-6 mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
            <p className="text-red-600 text-sm">{submitError}</p>
          </div>
        )}

        {/* Actions */}
        <div className="px-6 py-4 border-t border-rule flex gap-2.5">
          {step === "view" && (
            <>
              <button
                type="button"
                onClick={() => setStep("confirm")}
                className="flex-1 h-[46px] bg-ink text-white text-[13px] font-bold rounded-[10px]"
              >
                {t("confirm")} ✓
              </button>
              <button
                type="button"
                onClick={handleDecline}
                disabled={submitting}
                className="px-[18px] h-[46px] bg-white text-ink text-[12.5px] font-semibold border border-rule rounded-[10px] disabled:opacity-50"
              >
                {submitting ? <Spinner /> : t("decline")}
              </button>
            </>
          )}
          {step === "confirm" && (
            <button
              type="button"
              onClick={handleConfirm}
              disabled={submitting}
              className="flex-1 h-[46px] bg-ink text-white text-[13px] font-bold rounded-[10px] disabled:opacity-50 flex items-center justify-center gap-2"
            >
              {submitting && <Spinner />}
              {submitting ? t("confirming") : `${t("confirmSlot")} ✓`}
            </button>
          )}
        </div>
      </div>
    </Shell>
  );
}

function ConfirmCountdown({ expiresAt }: { expiresAt: string }) {
  const remaining = useCountdown(expiresAt);
  return (
    <div
      className="bg-red-50 text-red-800 px-3.5 py-2.5 rounded-[10px] mb-3 flex items-center gap-2 text-xs"
      role="timer"
      aria-live="polite"
    >
      <Clock size={14} />
      <span className="font-semibold">
        Deze link verloopt over <Mono>{remaining}</Mono>
      </span>
    </div>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-paper overflow-auto">
      <div className="max-w-[520px] mx-auto px-4 py-7">
        <div className="flex items-center gap-2 mb-[18px]">
          <LogoMark
            variant="green"
            className="h-[26px] w-[26px]"
            markPx={18}
            markStroke={11}
          />
          <span className="text-sm font-bold text-ink">CoachOS</span>
        </div>
        {children}
      </div>
    </div>
  );
}
