"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Plus } from "lucide-react";
import {
  getStandaloneLessons,
  STANDALONE_LESSON_LEVELS,
  type StandaloneLessonListItemDto,
} from "@/lib/api/standaloneLessons";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { SlashLabel } from "@/components/ui/slash-label";
import { Mono } from "@/components/ui/mono";
import { formatDateNL } from "@/lib/date-utils";

const DAYS_NL = ["zo", "ma", "di", "wo", "do", "vr", "za"];

function dayShort(isoDate: string): string {
  const d = new Date(isoDate + "T00:00:00");
  return DAYS_NL[d.getDay()];
}

function EmptyState() {
  const t = useTranslations("standaloneLessons");
  return (
    <div className="flex flex-col items-center justify-center py-24 px-6 text-center">
      <div className="w-20 h-20 rounded-full bg-tennis-green/[.08] flex items-center justify-center mb-6">
        <TennisBallEmptyIcon className="w-10 h-10" />
      </div>
      <p className="text-ink font-semibold text-base mb-1">{t("empty")}</p>
      <p className="text-ink-3 text-sm max-w-64 leading-relaxed mb-7">
        {t("emptyDescription")}
      </p>
      <Link
        href="/dashboard/standalone-lessons/new"
        className="inline-flex items-center gap-2 px-5 py-2.5 bg-ink text-white text-sm font-semibold rounded-lg"
      >
        <Plus size={14} />
        {t("create")}
      </Link>
    </div>
  );
}

function LessonRow({ lesson }: { lesson: StandaloneLessonListItemDto }) {
  const t = useTranslations("standaloneLessons");
  const levelLabel =
    lesson.level !== null ? STANDALONE_LESSON_LEVELS[lesson.level] : null;

  return (
    <Link
      href={`/dashboard/standalone-lessons/${lesson.id}`}
      className="grid grid-cols-[1.4fr_1.1fr_1.2fr_1.0fr_1.2fr_0.7fr] px-4 py-3.5 items-center border-b border-rule last:border-b-0 text-xs hover:bg-canvas/50 transition-colors"
    >
      <div>
        <p className="text-ink font-semibold text-[12px] m-0 capitalize">
          {dayShort(lesson.date)} {formatDateNL(lesson.date)}
        </p>
        <Mono className="text-[10.5px] text-ink-3 mt-0.5 block">
          {lesson.startTime} — {lesson.endTime}
        </Mono>
      </div>
      <Mono className="text-ink-2 text-[11px]">{lesson.courtName}</Mono>
      <span className="text-ink-2 text-[11.5px]">
        {lesson.trainerName ?? "—"}
      </span>
      <span className="text-[10.5px] text-ink-2">
        {levelLabel ? (
          <span className="px-2 py-0.5 rounded-full bg-canvas text-ink-2 font-semibold">
            {levelLabel}
          </span>
        ) : (
          "—"
        )}
      </span>
      <Mono className="text-ink-2 text-[11px]">
        {t("countConfirmed", {
          accepted: lesson.acceptedCount,
          total: lesson.invitedCount,
        })}
      </Mono>
      <div className="text-right">
        {lesson.isCancelled ? (
          <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-red-50 text-red-600 font-semibold">
            ○ geannuleerd
          </span>
        ) : (
          <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-tennis-green/10 text-tennis-green font-semibold">
            ● gepland
          </span>
        )}
      </div>
    </Link>
  );
}

export default function StandaloneLessonsPage() {
  const t = useTranslations("standaloneLessons");

  const {
    data: lessons,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["standaloneLessons"],
    queryFn: getStandaloneLessons,
  });

  const totalAccepted =
    lessons?.reduce((sum, l) => sum + l.acceptedCount, 0) ?? 0;
  const totalInvited =
    lessons?.reduce((sum, l) => sum + l.invitedCount, 0) ?? 0;

  return (
    <>
      {/* Page header */}
      <div className="flex items-center justify-between mb-5">
        <div>
          <SlashLabel>
            {lessons?.length ?? 0} les
            {(lessons?.length ?? 0) === 1 ? "" : "sen"} · {totalAccepted}/
            {totalInvited} bevestigd
          </SlashLabel>
          <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">
            {t("title")}
          </h1>
        </div>
        <div className="flex items-center gap-2.5">
          <Link
            href="/dashboard/standalone-lessons/new"
            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-ink text-white text-[11.5px] font-semibold rounded-md"
          >
            + {t("create")}
          </Link>
        </div>
      </div>

      {isLoading && (
        <div className="bg-paper border border-rule rounded-xl overflow-hidden animate-pulse">
          <div className="h-10 bg-canvas" />
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-16 border-t border-rule" />
          ))}
        </div>
      )}

      {isError && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-5 text-sm text-red-600">
          {t("loadError")}
        </div>
      )}

      {!isLoading && !isError && lessons?.length === 0 && <EmptyState />}

      {!isLoading && !isError && lessons && lessons.length > 0 && (
        <div className="bg-paper border border-rule rounded-xl overflow-hidden">
          {/* Column header */}
          <div className="grid grid-cols-[1.4fr_1.1fr_1.2fr_1.0fr_1.2fr_0.7fr] px-4 py-2.5 text-[10.5px] text-ink-3 font-semibold font-mono uppercase tracking-[0.08em] border-b border-rule bg-[#fbfaf6]">
            <span>Wanneer</span>
            <span>Baan</span>
            <span>Trainer</span>
            <span>Niveau</span>
            <span>Bevestigd</span>
            <span className="text-right">Status</span>
          </div>
          {lessons.map((l) => (
            <LessonRow key={l.id} lesson={l} />
          ))}
        </div>
      )}
    </>
  );
}
