"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { format, parseISO } from "date-fns";
import { nl } from "date-fns/locale";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  getMyLesson,
  type MyLessonDetailDto,
  type MyLessonParticipantStatus,
} from "@/lib/api/dashboard";
import { cn } from "@/lib/utils";

const STATUS_STYLE: Record<MyLessonParticipantStatus, string> = {
  confirmed: "bg-tennis-green/10 text-tennis-green",
  accepted: "bg-tennis-green/10 text-tennis-green",
  awaiting: "bg-amber-50 text-amber-700",
  pending: "bg-amber-50 text-amber-700",
  proposed: "bg-blue-50 text-blue-700",
  declined: "bg-red-50 text-red-600",
};

const STATUS_KEY: Record<MyLessonParticipantStatus, string> = {
  confirmed: "trainerPlayerConfirmed",
  awaiting: "trainerPlayerAwaiting",
  proposed: "trainerPlayerProposed",
  accepted: "trainerPlayerAccepted",
  pending: "trainerPlayerPending",
  declined: "trainerPlayerDeclined",
};

function InfoRow({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="grid grid-cols-[88px_1fr] gap-3 py-2 text-[13px]">
      <dt className="text-ink-3">{label}</dt>
      <dd className="text-ink min-w-0">{children}</dd>
    </div>
  );
}

function LessonBody({ lesson }: { lesson: MyLessonDetailDto }) {
  const t = useTranslations("dashboard");

  const href = lesson.seriesId
    ? `/dashboard/lessons/${lesson.seriesId}`
    : `/dashboard/standalone-lessons/${lesson.id}`;

  return (
    <>
      <dl className="divide-y divide-rule border-y border-rule">
        <InfoRow label={t("trainerLessonTime")}>
          <Mono className="font-medium">
            {lesson.startTime} – {lesson.endTime}
          </Mono>
          <span className="text-ink-3">
            {" "}
            · {t("trainerLessonDuration", { minutes: lesson.durationMinutes })}
          </span>
        </InfoRow>
        {lesson.tennisClubName && (
          <InfoRow label={t("trainerLessonClub")}>
            <p className="font-medium">{lesson.tennisClubName}</p>
            {lesson.tennisClubAddress && (
              <p className="text-[12px] text-ink-3">
                {lesson.tennisClubAddress}
              </p>
            )}
          </InfoRow>
        )}
        {lesson.courtName && (
          <InfoRow label={t("trainerLessonCourt")}>{lesson.courtName}</InfoRow>
        )}
        {lesson.level && (
          <InfoRow label={t("trainerLessonLevel")}>
            {t(`trainerLevel${lesson.level}`)}
          </InfoRow>
        )}
        {lesson.notes && (
          <InfoRow label={t("trainerLessonNotes")}>
            <span className="whitespace-pre-line">{lesson.notes}</span>
          </InfoRow>
        )}
      </dl>

      <section>
        <div className="flex items-center justify-between mb-2">
          <h3 className="text-[13px] font-bold text-ink">
            {t("trainerLessonPlayers")}
          </h3>
          <Mono className="text-[11px] text-ink-3">
            {t("trainerLessonPlayersCount", {
              count: lesson.participants.length,
              max: lesson.maxParticipants,
            })}
          </Mono>
        </div>

        {lesson.participants.length === 0 ? (
          <p className="rounded-lg border border-dashed border-rule px-3 py-4 text-center text-xs text-ink-3">
            {t("trainerLessonNoPlayers")}
          </p>
        ) : (
          <ul className="divide-y divide-rule rounded-lg border border-rule">
            {lesson.participants.map((p) => (
              <li
                key={`${p.name}-${p.status}`}
                className="flex items-center justify-between gap-3 px-3 py-2 text-[13px]"
              >
                <span className="truncate text-ink">{p.name}</span>
                <span
                  className={cn(
                    "shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold",
                    STATUS_STYLE[p.status],
                  )}
                >
                  {t(STATUS_KEY[p.status])}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <div className="flex justify-end">
        <Link
          href={href}
          className="inline-flex items-center rounded-md bg-ink px-3 py-1.5 text-[11.5px] font-semibold text-white hover:bg-ink/90 transition-colors"
        >
          {lesson.seriesId
            ? t("trainerLessonOpenSeries")
            : t("trainerLessonOpenStandalone")}
        </Link>
      </div>
    </>
  );
}

export function TrainerLessonDialog({
  lessonId,
  onClose,
}: {
  lessonId: string | null;
  onClose: () => void;
}) {
  const t = useTranslations("dashboard");

  const {
    data: lesson,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["dashboard", "my-lesson", lessonId],
    queryFn: () => getMyLesson(lessonId!),
    enabled: lessonId !== null,
  });

  return (
    <Dialog
      open={lessonId !== null}
      onOpenChange={(open) => !open && onClose()}
    >
      <DialogContent className="sm:max-w-md max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {lesson ? (lesson.seriesName ?? t("trainerStandaloneLesson")) : " "}
          </DialogTitle>
          <DialogDescription>
            {lesson
              ? format(parseISO(lesson.date), "EEEE d MMMM yyyy", {
                  locale: nl,
                })
              : " "}
          </DialogDescription>
        </DialogHeader>

        {isLoading && (
          <div className="space-y-3 animate-pulse">
            <div className="h-24 rounded-lg bg-canvas" />
            <div className="h-20 rounded-lg bg-canvas" />
          </div>
        )}
        {isError && (
          <p className="text-xs text-red-600">{t("trainerLessonLoadError")}</p>
        )}
        {lesson && <LessonBody lesson={lesson} />}
      </DialogContent>
    </Dialog>
  );
}
