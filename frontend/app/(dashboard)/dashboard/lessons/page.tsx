"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Plus } from "lucide-react";
import { getLessonSeries, type LessonSeriesDto } from "@/lib/api/lessonSeries";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { SlashLabel } from "@/components/ui/slash-label";
import { Mono } from "@/components/ui/mono";
import { OccupancyBar } from "@/components/ui/occupancy-bar";

function formatDateRange(start: string, end: string): string {
  const fmt = (d: string) => {
    const [y, m, day] = d.split("-");
    const months = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
    return `${day} ${months[parseInt(m, 10) - 1]}`;
  };
  return `${fmt(start)} → ${fmt(end)}`;
}

function EmptyState() {
  const t = useTranslations("lessonSeries");
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
        href="/dashboard/lessons/new"
        className="inline-flex items-center gap-2 px-5 py-2.5 bg-ink text-white text-sm font-semibold rounded-lg"
      >
        <Plus size={14} />
        {t("create")}
      </Link>
    </div>
  );
}

function SeriesRow({ series, index }: { series: LessonSeriesDto; index: number }) {
  const enrolled = series.enrolledCount ?? 0;
  const capacity = series.totalCapacity > 0 ? series.totalCapacity : 1;

  return (
    <Link
      href={`/dashboard/lessons/${series.id}`}
      className="grid grid-cols-[2.2fr_1fr_1.1fr_1.4fr_0.9fr_0.7fr] px-4 py-3.5 items-center border-b border-rule last:border-b-0 text-xs hover:bg-canvas/50 transition-colors"
    >
      <div>
        <p className="text-ink font-semibold text-[12px] m-0">{series.name}</p>
        <Mono className="text-[10.5px] text-ink-3 mt-0.5 block">
          {series.lessonCount} lesmomenten · reeks #{index + 1}
        </Mono>
      </div>
      <p className="text-ink-2 m-0">{series.trainerName || "—"}</p>
      <Mono className="text-ink-2 text-[11px]">
        {formatDateRange(series.startDate, series.endDate)}
      </Mono>
      <OccupancyBar filled={enrolled} capacity={capacity} />
      <Mono className="text-ink font-bold">€{series.price}</Mono>
      <div className="text-right">
        {series.isActive ? (
          <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-tennis-green/10 text-tennis-green font-semibold">
            ● actief
          </span>
        ) : (
          <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-canvas text-ink-3 font-semibold">
            ○ concept
          </span>
        )}
      </div>
    </Link>
  );
}

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

  const activeCount = series?.filter((s) => s.isActive).length ?? 0;
  const draftCount = series ? series.length - activeCount : 0;

  return (
    <>
      {/* Page header */}
      <div className="flex items-center justify-between mb-5">
        <div>
          <SlashLabel>
            {activeCount} actief · {draftCount} concept
          </SlashLabel>
          <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">
            {t("title")}
          </h1>
        </div>
        <div className="flex items-center gap-2.5">
          <Link
            href="/dashboard/lessons/new"
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
          Er ging iets mis bij het ophalen van de lessen.
        </div>
      )}

      {!isLoading && !isError && series?.length === 0 && <EmptyState />}

      {!isLoading && !isError && series && series.length > 0 && (
        <div className="bg-paper border border-rule rounded-xl overflow-hidden">
          {/* Column header */}
          <div className="grid grid-cols-[2.2fr_1fr_1.1fr_1.4fr_0.9fr_0.7fr] px-4 py-2.5 text-[10.5px] text-ink-3 font-semibold font-mono uppercase tracking-[0.08em] border-b border-rule bg-[#fbfaf6]">
            <span>Reeks</span>
            <span>Trainer</span>
            <span>Periode</span>
            <span>Bezetting</span>
            <span>Prijs</span>
            <span className="text-right">Status</span>
          </div>
          {series.map((s, i) => (
            <SeriesRow key={s.id} series={s} index={i} />
          ))}
        </div>
      )}
    </>
  );
}
