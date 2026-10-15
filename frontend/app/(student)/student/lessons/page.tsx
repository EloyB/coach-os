"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { ChevronRight, Clock } from "lucide-react";

import { getMyLessons, type StudentLesson } from "@/lib/api/student";
import { InkHeroCard } from "@/components/ui/ink-hero-card";
import { SlashLabel } from "@/components/ui/slash-label";
import { Mono } from "@/components/ui/mono";
import { Card, CardContent } from "@/components/ui/card";

// Backend-conventie: 0=maandag ... 6=zondag (zie CoachOS.Application/LessonSerie/LessonSerieService.cs).
const DAYS_NL = ["ma", "di", "wo", "do", "vr", "za", "zo"];
const STATUS_LABELS: Record<string, string> = {
  Confirmed: "Bevestigd",
  AwaitingConfirmation: "Wacht op bevestiging",
  Proposed: "Voorstel ontvangen",
};
const STATUS_DOTS: Record<string, string> = {
  Confirmed: "bg-tennis-green",
  AwaitingConfirmation: "bg-warn",
  Proposed: "bg-blue-500",
};

export default function StudentLessonsPage() {
  const t = useTranslations("studentPortal");
  const { data, isLoading, isError } = useQuery({
    queryKey: ["student", "lessons"],
    queryFn: getMyLessons,
  });

  if (isLoading) return <p className="text-ink-3">{t("loading")}</p>;
  if (isError) return <p className="text-urgent">{t("loadError")}</p>;

  const lessons = data ?? [];
  const nextLesson = lessons[0];

  return (
    <div>
      {/* Ink hero for next lesson */}
      {nextLesson && (
        <InkHeroCard className="p-[22px] mb-[18px]" showCourtLines>
          <SlashLabel className="text-tennis-lime">/eerstvolgende</SlashLabel>
          <h2 className="text-[22px] font-bold tracking-tight mt-1.5 mb-0.5">
            {DAYS_NL[nextLesson.dayOfWeek]} · {nextLesson.startTime}
          </h2>
          <p className="text-[13px] text-white/75 m-0">
            {nextLesson.seriesName}
            {nextLesson.trainerName && ` met ${nextLesson.trainerName}`}
            {nextLesson.courtName && ` · ${nextLesson.courtName}`}
          </p>

          {nextLesson.status === "AwaitingConfirmation" && (
            <div className="mt-3.5 inline-flex items-center gap-1.5 px-3 py-1.5 bg-white/[.12] rounded-full text-[11.5px]">
              <Clock size={12} className="text-tennis-lime" />
              Bevestiging nodig
            </div>
          )}

          <div className="mt-4 flex gap-2">
            {nextLesson.status === "AwaitingConfirmation" ? (
              <>
                <Link
                  href={`/student/lessons/${nextLesson.assignmentId}`}
                  className="px-4 py-2.5 bg-tennis-lime text-tennis-green rounded-lg text-[12.5px] font-bold"
                >
                  ✓ Ik kom
                </Link>
                <Link
                  href={`/student/lessons/${nextLesson.assignmentId}?action=reschedule`}
                  className="px-3.5 py-2.5 bg-white/10 text-white border border-white/20 rounded-lg text-[12.5px] font-medium"
                >
                  Vraag ruil
                </Link>
              </>
            ) : (
              <Link
                href={`/student/lessons/${nextLesson.assignmentId}`}
                className="px-4 py-2.5 bg-white/10 text-white border border-white/20 rounded-lg text-[12.5px] font-medium"
              >
                Bekijk details →
              </Link>
            )}
          </div>
        </InkHeroCard>
      )}

      {/* Lesson list */}
      <p className="text-[11px] text-ink-3 font-bold uppercase tracking-[0.08em] mb-2.5 mt-1">
        {t("title")}
      </p>

      {lessons.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center text-ink-3">
            {t("empty")}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {lessons.map((lesson) => {
            const dotCls = STATUS_DOTS[lesson.status] ?? "bg-ink-3";
            const statusLabel = STATUS_LABELS[lesson.status] ?? lesson.status;
            const dayLabel = DAYS_NL[lesson.dayOfWeek];

            return (
              <Link
                key={lesson.assignmentId}
                href={`/student/lessons/${lesson.assignmentId}`}
                className="flex items-center gap-3.5 p-3.5 bg-paper border border-rule rounded-[10px] hover:bg-canvas/50 transition-colors"
              >
                <div className="w-11 text-center py-1.5 bg-canvas rounded-md">
                  <p className="text-[9.5px] text-ink-3 uppercase font-semibold m-0">
                    {dayLabel}
                  </p>
                  <Mono className="text-[13px] font-bold text-ink">
                    {lesson.startTime?.substring(0, 5)}
                  </Mono>
                </div>
                <div className="flex-1 min-w-0">
                  {lesson.participantName && (
                    <p className="text-[11px] text-ink-3 m-0">
                      {lesson.participantName}
                    </p>
                  )}
                  <p className="text-[12.5px] font-semibold text-ink m-0">
                    {lesson.seriesName}
                  </p>
                  <p className="text-[11px] text-ink-3 flex items-center gap-1.5 mt-0.5 m-0">
                    <span className={`w-1.5 h-1.5 rounded-full ${dotCls}`} />
                    {statusLabel}
                  </p>
                </div>
                <ChevronRight size={14} className="text-ink-3 shrink-0" />
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
