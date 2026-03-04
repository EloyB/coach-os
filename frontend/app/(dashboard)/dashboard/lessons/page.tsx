"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  Plus,
  ChevronRight,
  Clock,
  UserCheck,
  Euro,
  BarChart2,
} from "lucide-react";
import { getLessonSeries, LESSON_LEVELS, LessonSeriesDto } from "@/lib/api/lessonSeries";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";

// ─── Skeleton ────────────────────────────────────────────────────────────────

function SkeletonCard() {
  return (
    <div className="bg-white rounded-xl border-l-4 border-l-gray-200 shadow-sm p-5 animate-pulse">
      <div className="flex items-start justify-between mb-3">
        <div className="h-5 bg-gray-200 rounded w-2/3" />
        <div className="h-5 bg-gray-100 rounded-full w-16" />
      </div>
      <div className="h-3.5 bg-gray-100 rounded w-1/3 mb-4" />
      <div className="h-3 bg-gray-100 rounded w-1/2 mb-5" />
      <div className="flex gap-3 mb-4">
        <div className="h-8 bg-gray-100 rounded-lg flex-1" />
        <div className="h-8 bg-gray-100 rounded-lg flex-1" />
        <div className="h-8 bg-gray-100 rounded-lg flex-1" />
      </div>
      <div className="h-px bg-gray-100 mb-3" />
      <div className="flex items-center justify-between">
        <div className="h-3.5 bg-gray-100 rounded w-24" />
        <div className="h-3.5 bg-gray-100 rounded w-16" />
      </div>
    </div>
  );
}

// ─── Empty State ─────────────────────────────────────────────────────────────

function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center py-24 px-6 text-center">
      <div className="w-20 h-20 rounded-full bg-tennis-green/8 flex items-center justify-center mb-6">
        <TennisBallEmptyIcon className="w-10 h-10" />
      </div>
      <p className="text-gray-700 font-semibold text-base mb-1">
        Nog geen lesreeksen
      </p>
      <p className="text-gray-400 text-sm max-w-64 leading-relaxed mb-7">
        Maak je eerste lesreeks aan en begin met het plannen van lesmomenten.
      </p>
      <Link
        href="/dashboard/lessons/new"
        className="inline-flex items-center gap-2 px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
      >
        <Plus size={14} />
        Nieuwe lesreeks
      </Link>
    </div>
  );
}

// ─── Series Card ─────────────────────────────────────────────────────────────

function SeriesCard({ series }: { series: LessonSeriesDto }) {
  return (
    <div className="bg-white rounded-xl border-l-4 border-l-tennis-green shadow-sm shadow-gray-100 p-5 flex flex-col hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between gap-3 mb-1">
        <h3 className="font-semibold text-gray-900 text-sm leading-snug">
          {series.name}
        </h3>
        <span
          className={`shrink-0 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
            series.isActive
              ? "bg-green-50 text-green-700"
              : "bg-gray-100 text-gray-500"
          }`}
        >
          {series.isActive ? "Actief" : "Inactief"}
        </span>
      </div>

      <p className="text-xs text-gray-400 mb-2 flex items-center gap-1">
        <UserCheck size={11} className="shrink-0" />
        {series.trainerName || "—"}
      </p>

      <p className="text-xs text-gray-500 mb-4">
        {series.startDate} &rarr; {series.endDate}
      </p>

      <div className="grid grid-cols-4 gap-2 mb-4">
        <div className="bg-[#F5F4F1] rounded-lg px-2 py-2 text-center">
          <BarChart2 size={11} className="mx-auto text-tennis-green mb-1" />
          <p className="text-[10px] text-gray-400 leading-none mb-0.5">Niveau</p>
          <p className="text-xs font-semibold text-gray-700 leading-none truncate">
            {LESSON_LEVELS[series.level] ?? `N${series.level}`}
          </p>
        </div>
        <div className="bg-[#F5F4F1] rounded-lg px-2 py-2 text-center">
          <Euro size={11} className="mx-auto text-tennis-green mb-1" />
          <p className="text-[10px] text-gray-400 leading-none mb-0.5">Prijs</p>
          <p className="text-xs font-semibold text-gray-700 leading-none">
            €{series.price}
          </p>
        </div>
        <div className="bg-[#F5F4F1] rounded-lg px-2 py-2 text-center">
          <Clock size={11} className="mx-auto text-tennis-green mb-1" />
          <p className="text-[10px] text-gray-400 leading-none mb-0.5">Duur</p>
          <p className="text-xs font-semibold text-gray-700 leading-none">
            {series.durationMinutes}m
          </p>
        </div>
      </div>

      <div className="mt-auto pt-3 border-t border-gray-100 flex items-center justify-between">
        <span className="text-xs text-gray-400">
          {series.lessonCount}{" "}
          {series.lessonCount === 1 ? "lesmoment" : "lesmomenten"}
        </span>
        <Link
          href={`/dashboard/lessons/${series.id}`}
          className="flex items-center gap-1 text-xs font-medium text-tennis-green hover:text-tennis-green/70 transition-colors"
        >
          Bekijken
          <ChevronRight size={13} />
        </Link>
      </div>
    </div>
  );
}

// ─── Page ────────────────────────────────────────────────────────────────────

export default function LessonsPage() {
  const t = useTranslations("lessonSeries");

  const {
    data: series,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["lessonSeries"],
    queryFn: getLessonSeries,
  });

  return (
    <>
      {/* Page header */}
      <div className="flex items-start justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
            {t("title")}
          </h1>
          <p className="text-gray-400 text-sm mt-1">{t("subtitle")}</p>
        </div>
        <Link
          href="/dashboard/lessons/new"
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors shrink-0"
        >
          <Plus size={14} />
          {t("create")}
        </Link>
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
      )}

      {isError && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-5 text-sm text-red-600">
          Er ging iets mis bij het ophalen van de lessen. Probeer de pagina te
          verversen.
        </div>
      )}

      {!isLoading && !isError && series?.length === 0 && <EmptyState />}

      {!isLoading && !isError && series && series.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {series.map((s) => (
            <SeriesCard key={s.id} series={s} />
          ))}
        </div>
      )}
    </>
  );
}
