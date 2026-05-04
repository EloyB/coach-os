"use client";

import { use, useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import {
  CheckCircle2,
  XCircle,
  Clock,
  Calendar,
  Building2,
  GraduationCap,
} from "lucide-react";
import {
  getPublicInvitation,
  acceptPublicInvitation,
  declinePublicInvitation,
  type PublicInvitationDto,
} from "@/lib/api/invitations";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import { STANDALONE_LESSON_LEVELS } from "@/lib/api/standaloneLessons";
import { formatDateNL } from "@/lib/date-utils";

const DAYS_LONG_NL = [
  "zondag",
  "maandag",
  "dinsdag",
  "woensdag",
  "donderdag",
  "vrijdag",
  "zaterdag",
];

function dayLong(isoDate: string): string {
  const d = new Date(isoDate + "T00:00:00");
  return DAYS_LONG_NL[d.getDay()];
}

function Spinner() {
  return (
    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
      <circle
        className="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        strokeWidth="4"
      />
      <path
        className="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
      />
    </svg>
  );
}

export default function InvitationPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = use(params);
  const t = useTranslations("invitations");

  const [details, setDetails] = useState<PublicInvitationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorState, setErrorState] = useState<"not-found" | null>(null);
  const [submitting, setSubmitting] = useState<"accept" | "decline" | null>(
    null
  );

  useEffect(() => {
    async function load() {
      try {
        const data = await getPublicInvitation(token);
        setDetails(data);
      } catch {
        setErrorState("not-found");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [token]);

  async function handleAccept() {
    if (!details) return;
    setSubmitting("accept");
    try {
      const updated = await acceptPublicInvitation(token);
      setDetails(updated);
    } catch {
      // toast handled by api-client interceptor
    } finally {
      setSubmitting(null);
    }
  }

  async function handleDecline() {
    if (!details) return;
    setSubmitting("decline");
    try {
      const updated = await declinePublicInvitation(token);
      setDetails(updated);
    } catch {
      // toast handled by api-client interceptor
    } finally {
      setSubmitting(null);
    }
  }

  return (
    <div className="min-h-screen flex">
      {/* Left panel — ink branding */}
      <div className="hidden lg:flex lg:w-[44%] bg-ink relative overflow-hidden flex-col justify-between p-10">
        <CourtLines opacity={0.05} />

        <div
          className="absolute -right-[120px] -top-[80px] w-[320px] h-[320px] rounded-full blur-[10px]"
          style={{
            background:
              "radial-gradient(closest-side, rgba(45,80,22,.55), transparent)",
          }}
        />

        <div className="relative z-10 flex items-center gap-2.5">
          <div className="w-[30px] h-[30px] rounded-md bg-tennis-lime grid place-items-center">
            <Mono className="text-ink font-extrabold text-[13px]">c/</Mono>
          </div>
          <span className="text-white font-bold text-base">CoachOS</span>
        </div>

        <div className="relative z-10">
          <SlashLabel className="text-tennis-lime mb-2.5">
            {t("heroLabel")}
          </SlashLabel>
          <h1 className="text-[34px] font-extrabold text-white leading-[1.05] tracking-tight mb-3">
            {t("headline")}
          </h1>
          <p className="text-[13px] text-[#a8a195] max-w-[300px] leading-relaxed">
            {t("heroTagline")}
          </p>
        </div>

        <div className="relative z-10" />
      </div>

      {/* Right panel — content */}
      <div className="w-full lg:w-[56%] flex items-center justify-center p-8 bg-paper">
        <div className="w-full max-w-[420px]">
          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-8 lg:hidden">
            <div className="w-7 h-7 rounded-md bg-tennis-green grid place-items-center">
              <Mono className="text-tennis-lime font-extrabold text-[12px]">
                c/
              </Mono>
            </div>
            <span className="text-ink text-xl font-bold">CoachOS</span>
          </div>

          {loading && (
            <div className="space-y-3 animate-pulse">
              <div className="h-4 w-24 bg-canvas rounded" />
              <div className="h-7 w-2/3 bg-canvas rounded" />
              <div className="h-32 bg-canvas rounded-lg" />
              <div className="h-10 bg-canvas rounded-lg" />
              <div className="h-10 bg-canvas rounded-lg" />
            </div>
          )}

          {!loading && errorState === "not-found" && (
            <div>
              <SlashLabel>{t("heroLabel")}</SlashLabel>
              <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
                {t("notFound")}
              </h2>
              <p className="text-[12.5px] text-ink-3">{t("notFoundDesc")}</p>
            </div>
          )}

          {!loading && details && details.isCancelled && (
            <div>
              <SlashLabel>{t("heroLabel")}</SlashLabel>
              <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
                {t("lessonCancelled")}
              </h2>
              <p className="text-[12.5px] text-ink-3">
                {t("lessonCancelledDesc")}
              </p>
            </div>
          )}

          {!loading && details && !details.isCancelled && details.status === 1 && (
            <div>
              <div className="w-14 h-14 rounded-full bg-tennis-green/[.12] grid place-items-center mb-5">
                <CheckCircle2 className="w-7 h-7 text-tennis-green" />
              </div>
              <SlashLabel>{t("heroLabel")}</SlashLabel>
              <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
                {t("successAcceptedTitle")}
              </h2>
              <p className="text-[12.5px] text-ink-3">
                {t("successAcceptedBody", {
                  date: `${dayLong(details.date)} ${formatDateNL(details.date)}`,
                  time: details.startTime,
                })}
              </p>
            </div>
          )}

          {!loading && details && !details.isCancelled && details.status === 2 && (
            <div>
              <div className="w-14 h-14 rounded-full bg-canvas grid place-items-center mb-5">
                <XCircle className="w-7 h-7 text-ink-3" />
              </div>
              <SlashLabel>{t("heroLabel")}</SlashLabel>
              <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
                {t("successDeclinedTitle")}
              </h2>
              <p className="text-[12.5px] text-ink-3">
                {t("successDeclinedBody")}
              </p>
            </div>
          )}

          {!loading && details && !details.isCancelled && details.status === 0 && (
            <>
              <SlashLabel>{t("heroLabel")}</SlashLabel>
              <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1">
                {t("headline")}
              </h2>
              <p className="text-[12.5px] text-ink-3 mb-5">{t("subhead")}</p>

              {/* Lesson card */}
              <div className="bg-canvas/60 border border-rule rounded-lg p-4 mb-5">
                <div className="flex items-center gap-2 mb-3">
                  <Calendar size={14} className="text-tennis-green" />
                  <Mono className="text-[12px] font-semibold text-ink capitalize">
                    {dayLong(details.date)} {formatDateNL(details.date)}
                  </Mono>
                </div>
                <div className="flex items-center gap-2 mb-3">
                  <Clock size={14} className="text-tennis-green" />
                  <Mono className="text-[12px] text-ink-2">
                    {details.startTime} — {details.endTime} (
                    {t("durationMinutes", { minutes: details.durationMinutes })})
                  </Mono>
                </div>
                <div className="flex items-center gap-2 mb-3">
                  <Building2 size={14} className="text-tennis-green" />
                  <span className="text-[12px] text-ink-2">
                    {t("court")}: {details.courtName}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <GraduationCap size={14} className="text-tennis-green" />
                  <span className="text-[12px] text-ink-2">
                    {t("trainer")}: {details.trainerName}
                    {details.level !== null &&
                      ` · ${t("level")}: ${STANDALONE_LESSON_LEVELS[details.level]}`}
                  </span>
                </div>
              </div>

              {details.notes && (
                <div className="mb-5">
                  <p className="text-[10.5px] uppercase tracking-[0.08em] text-ink-3 font-semibold mb-1">
                    {t("notes")}
                  </p>
                  <p className="text-[12.5px] text-ink-2 italic m-0 whitespace-pre-wrap">
                    {details.notes}
                  </p>
                </div>
              )}

              {/* Action buttons */}
              <div className="space-y-2.5">
                <button
                  type="button"
                  onClick={handleAccept}
                  disabled={submitting !== null}
                  className="w-full inline-flex items-center justify-center gap-2 px-4 py-3 bg-tennis-green text-tennis-lime text-[13px] font-semibold rounded-lg disabled:opacity-50"
                >
                  {submitting === "accept" ? <Spinner /> : null}
                  {t("confirm")}
                </button>
                <button
                  type="button"
                  onClick={handleDecline}
                  disabled={submitting !== null}
                  className="w-full inline-flex items-center justify-center gap-2 px-4 py-3 border border-rule text-ink-2 text-[13px] font-semibold rounded-lg hover:bg-canvas/50 disabled:opacity-50"
                >
                  {submitting === "decline" ? <Spinner /> : null}
                  {t("decline")}
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
