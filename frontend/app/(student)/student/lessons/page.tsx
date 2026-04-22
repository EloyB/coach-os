"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useTranslations } from "next-intl";

import { getMyLessons, type StudentLesson } from "@/lib/api/student";
import { Card, CardContent } from "@/components/ui/card";
import {
  assignmentStatusStyles,
  assignmentStatusFallback,
} from "@/lib/status-styles";

const DAYS_NL = ["zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag"];

function StatusBadge({ status }: { status: string }) {
  const cls = assignmentStatusStyles[status] ?? assignmentStatusFallback;
  return (
    <span className={`inline-block px-2.5 py-1 rounded-full text-xs font-semibold ${cls}`}>
      {status}
    </span>
  );
}

export default function StudentLessonsPage() {
  const t = useTranslations("studentPortal");
  const { data, isLoading, isError } = useQuery({
    queryKey: ["student", "lessons"],
    queryFn: getMyLessons,
  });

  if (isLoading) return <p className="text-gray-500">{t("loading")}</p>;
  if (isError) return <p className="text-red-600">{t("loadError")}</p>;

  const lessons = data ?? [];

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">{t("title")}</h1>
        <p className="text-gray-500 text-sm mt-1">{t("subtitle")}</p>
      </div>

      {lessons.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center text-gray-500">
            {t("empty")}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {lessons.map((lesson) => (
            <LessonRow key={lesson.assignmentId} lesson={lesson} />
          ))}
        </div>
      )}
    </div>
  );
}

function LessonRow({ lesson }: { lesson: StudentLesson }) {
  return (
    <Link href={`/student/lessons/${lesson.assignmentId}`} className="block">
      <Card className="border-l-4 border-l-tennis-lime hover:shadow-md transition-shadow cursor-pointer">
        <CardContent className="py-4 flex items-center justify-between gap-4">
          <div>
            <p className="font-semibold text-gray-900">{lesson.seriesName}</p>
            <p className="text-sm text-gray-500 mt-0.5">
              {DAYS_NL[lesson.dayOfWeek]} {lesson.startTime} — {lesson.endTime}
              {lesson.courtName && ` · ${lesson.courtName}`}
            </p>
            {lesson.trainerName && (
              <p className="text-xs text-gray-400 mt-0.5">{lesson.trainerName}</p>
            )}
          </div>
          <StatusBadge status={lesson.status} />
        </CardContent>
      </Card>
    </Link>
  );
}
