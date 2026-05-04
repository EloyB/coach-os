"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import {
  ChevronLeft,
  Clock,
  Users,
  CalendarDays,
  Building2,
  GraduationCap,
  RefreshCw,
  Plus,
  AlertTriangle,
  X,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  getStandaloneLesson,
  cancelStandaloneLesson,
  addInvitations,
  resendInvitation,
  STANDALONE_LESSON_LEVELS,
  type InvitationDto,
} from "@/lib/api/standaloneLessons";
import { SlashLabel } from "@/components/ui/slash-label";
import { Mono } from "@/components/ui/mono";
import { formatDateNL } from "@/lib/date-utils";
import { inputClass } from "@/lib/styles";

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

function statusBadge(
  status: number,
  t: ReturnType<typeof useTranslations<"standaloneLessons">>
) {
  if (status === 1) {
    return (
      <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-tennis-green/10 text-tennis-green font-semibold">
        {t("statusAccepted")}
      </span>
    );
  }
  if (status === 2) {
    return (
      <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-red-50 text-red-600 font-semibold">
        {t("statusDeclined")}
      </span>
    );
  }
  return (
    <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 font-semibold">
      {t("statusInvited")}
    </span>
  );
}

function emailInitials(email: string): string {
  const localPart = email.split("@")[0] ?? "";
  if (!localPart) return "?";
  const parts = localPart.split(/[._-]/).filter(Boolean);
  if (parts.length >= 2)
    return (parts[0][0] + parts[1][0]).toUpperCase();
  return localPart.slice(0, 2).toUpperCase();
}

function InvitationRow({
  invitation,
  lessonId,
  isLessonCancelled,
}: {
  invitation: InvitationDto;
  lessonId: string;
  isLessonCancelled: boolean;
}) {
  const t = useTranslations("standaloneLessons");
  const queryClient = useQueryClient();

  const resendMutation = useMutation({
    mutationFn: () => resendInvitation(lessonId, invitation.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["standaloneLessons", lessonId] });
      toast.success(t("detailResendSuccess"));
    },
  });

  const showResend = !isLessonCancelled && invitation.status !== 1;

  return (
    <div className="flex items-center gap-3 px-3 py-2.5 border-b border-rule last:border-b-0">
      <div className="w-8 h-8 rounded-full bg-tennis-green/[.08] grid place-items-center shrink-0">
        <Mono className="text-[10px] text-tennis-green font-bold">
          {emailInitials(invitation.email)}
        </Mono>
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-[12px] text-ink font-medium truncate m-0">
          {invitation.email}
        </p>
      </div>
      {statusBadge(invitation.status, t)}
      {showResend && (
        <button
          type="button"
          disabled={resendMutation.isPending}
          onClick={() => resendMutation.mutate()}
          className="text-[10.5px] text-ink-3 hover:text-ink inline-flex items-center gap-1 px-2 py-1 rounded hover:bg-canvas disabled:opacity-50"
          aria-label={t("detailResend")}
        >
          <RefreshCw size={11} />
          {t("detailResend")}
        </button>
      )}
    </div>
  );
}

function AddInvitationRow({ lessonId }: { lessonId: string }) {
  const t = useTranslations("standaloneLessons");
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [email, setEmail] = useState("");

  const addMutation = useMutation({
    mutationFn: (em: string) => addInvitations(lessonId, [em]),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["standaloneLessons", lessonId] });
      queryClient.invalidateQueries({ queryKey: ["standaloneLessons"] });
      toast.success(t("detailInviteAdded"));
      setEmail("");
      setOpen(false);
    },
  });

  function submit() {
    const trimmed = email.trim().toLowerCase();
    if (!trimmed) return;
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)) {
      toast.error("Ongeldig e-mailadres");
      return;
    }
    addMutation.mutate(trimmed);
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="w-full text-left px-3 py-2.5 text-[12px] text-tennis-green font-semibold hover:bg-tennis-green/[.04] inline-flex items-center gap-1.5"
      >
        <Plus size={13} /> {t("detailInviteMore")}
      </button>
    );
  }

  return (
    <div className="flex items-center gap-2 px-3 py-2.5 bg-canvas/60">
      <input
        type="email"
        autoFocus
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        onKeyDown={(e) => e.key === "Enter" && submit()}
        placeholder={t("detailInvitePlaceholder")}
        className={inputClass + " text-[12px] flex-1"}
      />
      <button
        type="button"
        onClick={submit}
        disabled={addMutation.isPending}
        className="px-3 py-1.5 bg-ink text-white text-[11.5px] font-semibold rounded-md disabled:opacity-50"
      >
        {t("detailInviteSubmit")}
      </button>
      <button
        type="button"
        onClick={() => {
          setOpen(false);
          setEmail("");
        }}
        className="px-2 py-1.5 text-[11.5px] text-ink-3 hover:text-ink"
      >
        {t("detailInviteCancel")}
      </button>
    </div>
  );
}

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Clock;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-start gap-3 py-2">
      <Icon size={14} className="text-ink-3 mt-0.5 shrink-0" />
      <div className="flex-1 min-w-0">
        <p className="text-[10.5px] uppercase tracking-[0.08em] text-ink-3 font-semibold m-0">
          {label}
        </p>
        <div className="text-[12.5px] text-ink mt-0.5">{value}</div>
      </div>
    </div>
  );
}

export default function StandaloneLessonDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("standaloneLessons");
  const queryClient = useQueryClient();

  const {
    data: lesson,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["standaloneLessons", id],
    queryFn: () => getStandaloneLesson(id),
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelStandaloneLesson(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["standaloneLessons"] });
      queryClient.invalidateQueries({ queryKey: ["standaloneLessons", id] });
      toast.success(t("detailCancelSuccess"));
    },
  });

  if (isLoading) {
    return (
      <div className="bg-paper border border-rule rounded-xl p-6 animate-pulse h-64" />
    );
  }

  if (isError || !lesson) {
    return (
      <div className="bg-red-50 border border-red-100 rounded-xl p-5 text-sm text-red-600">
        {t("detailNotFound")}
      </div>
    );
  }

  const acceptedCount = lesson.invitations.filter(
    (i) => i.status === 1
  ).length;
  const levelLabel =
    lesson.level !== null ? STANDALONE_LESSON_LEVELS[lesson.level] : null;

  return (
    <>
      {/* Page header */}
      <div className="mb-5">
        <Link
          href="/dashboard/standalone-lessons"
          className="inline-flex items-center gap-1 text-[11.5px] text-ink-3 hover:text-ink mb-2"
        >
          <ChevronLeft size={14} /> {t("back")}
        </Link>
        <div className="flex items-center justify-between">
          <div>
            <SlashLabel>
              <span className="capitalize">{dayLong(lesson.date)}</span> ·{" "}
              {formatDateNL(lesson.date)} · {lesson.startTime}
            </SlashLabel>
            <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">
              {lesson.courtName}
            </h1>
          </div>
          {lesson.isCancelled ? (
            <span className="text-[11px] px-2.5 py-1 rounded-full bg-red-50 text-red-600 font-semibold inline-flex items-center gap-1.5">
              <AlertTriangle size={11} /> {t("detailCancelled")}
            </span>
          ) : (
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <button
                  type="button"
                  className="text-[11.5px] text-red-600 hover:bg-red-50 rounded-md px-3 py-1.5 inline-flex items-center gap-1.5"
                >
                  <X size={12} /> {t("detailCancel")}
                </button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>
                    {t("detailCancelConfirmTitle")}
                  </AlertDialogTitle>
                  <AlertDialogDescription>
                    {t("detailCancelConfirmDesc")}
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Annuleren</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={() => cancelMutation.mutate()}
                    className="bg-red-600 hover:bg-red-700"
                  >
                    {t("detailCancel")}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[3fr_2fr] gap-5">
        {/* Lesson info card */}
        <div className="bg-paper border border-rule rounded-xl p-5">
          <p className="text-[10.5px] uppercase tracking-[0.08em] text-ink-3 font-semibold mb-3">
            {t("detailLessonInfo")}
          </p>
          <div className="divide-y divide-rule">
            <InfoRow
              icon={CalendarDays}
              label={t("fieldDate")}
              value={
                <span className="capitalize">
                  {dayLong(lesson.date)} {formatDateNL(lesson.date)}
                </span>
              }
            />
            <InfoRow
              icon={Clock}
              label={t("fieldDuration")}
              value={
                <Mono>
                  {lesson.startTime} — {lesson.endTime} ({lesson.durationMinutes}{" "}
                  min)
                </Mono>
              }
            />
            <InfoRow
              icon={Building2}
              label={t("fieldCourt")}
              value={lesson.courtName}
            />
            <InfoRow
              icon={GraduationCap}
              label={t("fieldTrainer")}
              value={lesson.trainerName ?? "—"}
            />
            <InfoRow
              icon={Users}
              label={t("fieldMaxParticipants")}
              value={
                <Mono>
                  {acceptedCount} / {lesson.maxParticipants > 0 ? lesson.maxParticipants : "∞"}{" "}
                  bevestigd
                </Mono>
              }
            />
            {levelLabel && (
              <InfoRow
                icon={GraduationCap}
                label={t("fieldLevel")}
                value={levelLabel}
              />
            )}
          </div>
          {lesson.notes && (
            <div className="mt-4 p-3 bg-canvas/60 rounded-lg border border-rule">
              <p className="text-[10.5px] uppercase tracking-[0.08em] text-ink-3 font-semibold mb-1">
                {t("fieldNotes")}
              </p>
              <p className="text-[12.5px] text-ink-2 m-0 whitespace-pre-wrap">
                {lesson.notes}
              </p>
            </div>
          )}
        </div>

        {/* Participants card */}
        <div className="bg-paper border border-rule rounded-xl overflow-hidden">
          <div className="px-5 py-3.5 border-b border-rule flex items-center justify-between">
            <p className="text-[10.5px] uppercase tracking-[0.08em] text-ink-3 font-semibold m-0">
              {t("detailParticipants")}
            </p>
            <Mono className="text-[11px] text-ink-2">
              {t("countConfirmed", {
                accepted: acceptedCount,
                total: lesson.invitations.length,
              })}
            </Mono>
          </div>
          <div>
            {lesson.invitations.map((inv) => (
              <InvitationRow
                key={inv.id}
                invitation={inv}
                lessonId={lesson.id}
                isLessonCancelled={lesson.isCancelled}
              />
            ))}
            {!lesson.isCancelled && <AddInvitationRow lessonId={lesson.id} />}
          </div>
        </div>
      </div>
    </>
  );
}
