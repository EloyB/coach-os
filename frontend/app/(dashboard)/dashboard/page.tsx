"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Plus, Clock, Calendar } from "lucide-react";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { StatStrip } from "@/components/ui/stat-strip";
import { SlashLabel } from "@/components/ui/slash-label";
import { Mono } from "@/components/ui/mono";
import { Sparkline } from "@/components/ui/sparkline";
import {
  getDashboardSummary,
  getDashboardInbox,
  getDashboardMetrics,
} from "@/lib/api/dashboard";
import type { InboxItemDto, InboxResponse } from "@/lib/api/dashboard";
import { useTranslations } from "next-intl";

function formatDate(dateStr: string): string {
  const [year, month, day] = dateStr.split("-");
  return `${day}/${month}/${year}`;
}

function formatTime(timeStr: string): string {
  return timeStr.substring(0, 5);
}

function severityDotColor(severity: string): string {
  switch (severity) {
    case "urgent":
      return "bg-red-500";
    case "warn":
      return "bg-amber-400";
    default:
      return "bg-blue-300";
  }
}

function inboxItemHref(item: InboxItemDto): string {
  if (item.type === "confirmation_pending") {
    return `/dashboard/lessons/${item.refId}/planning`;
  }
  if (item.type === "series_underbooked") {
    return `/dashboard/lessons/${item.refId}#enrollments`;
  }
  if (item.refType === "Student") return `/dashboard/lessons/${item.refId}`;
  return `/dashboard/lessons/${item.refId}`;
}

function InboxItem({ item }: { item: InboxItemDto }) {
  const t = useTranslations("dashboard");

  return (
    <div className="flex items-center justify-between px-4 py-3 bg-paper border border-rule rounded-lg">
      <div className="min-w-0 flex-1">
        <p className="text-[12.5px] font-semibold text-ink truncate flex items-center gap-2">
          <span
            aria-hidden
            className={`inline-block w-2 h-2 rounded-full shrink-0 ${severityDotColor(item.severity)}`}
          />
          <span className="truncate">{item.title}</span>
        </p>
        <p className="text-[11px] text-ink-3 truncate mt-0.5">{item.body}</p>
        {item.meta && (
          <Mono className="text-[10px] text-ink-3 mt-1 block">{item.meta}</Mono>
        )}
      </div>
      <Link
        href={inboxItemHref(item)}
        className="ml-3 shrink-0 px-3 py-1.5 text-[10.5px] text-white font-semibold bg-ink rounded hover:bg-ink/90 transition-colors"
      >
        {t("inboxResolve")}
      </Link>
    </div>
  );
}

const INBOX_COLLAPSED_COUNT = 3;

function InboxSection({ inbox }: { inbox: InboxResponse | undefined }) {
  const t = useTranslations("dashboard");
  const [expanded, setExpanded] = useState(false);

  const items = inbox?.items ?? [];
  const hasItems = items.length > 0;
  const hasMore = items.length > INBOX_COLLAPSED_COUNT;
  const visibleItems = expanded ? items : items.slice(0, INBOX_COLLAPSED_COUNT);

  return (
    <div className="bg-paper border border-rule rounded-xl p-[18px] mb-[18px]">
      <div className="flex items-center justify-between mb-3">
        <div>
          <SlashLabel>{t("priorityLabel")}</SlashLabel>
          <p className="text-sm font-bold text-ink mt-0.5">
            {hasItems
              ? `${items.length} dingen om nu aan te pakken`
              : t("priorityEmpty")}
          </p>
        </div>
      </div>
      {hasItems ? (
        <>
          <div className="space-y-2">
            {visibleItems.map((item, idx) => (
              <InboxItem key={`${item.refType}-${item.refId}-${idx}`} item={item} />
            ))}
          </div>
          {hasMore && (
            <button
              type="button"
              onClick={() => setExpanded((v) => !v)}
              className="mt-2.5 text-[11px] text-ink-3 font-medium hover:text-ink transition-colors cursor-pointer"
            >
              {expanded
                ? "Minder tonen"
                : `+ ${items.length - INBOX_COLLAPSED_COUNT} meer tonen`}
            </button>
          )}
        </>
      ) : (
        <div className="text-[12.5px] text-ink-3 text-center py-4 border border-dashed border-rule rounded-lg">
          {t("priorityEmptyDesc")}
        </div>
      )}
    </div>
  );
}

export default function DashboardPage() {
  const t = useTranslations("dashboard");
  const { data: summary, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: getDashboardSummary,
  });

  const { data: inbox } = useQuery({
    queryKey: ["dashboard", "inbox"],
    queryFn: () => getDashboardInbox(),
  });

  const { data: metrics } = useQuery({
    queryKey: ["dashboard", "metrics"],
    queryFn: () => getDashboardMetrics(),
  });

  const statItems = [
    {
      value: String(summary?.lessonsTodayCount ?? 0),
      label: t("statLessonsToday"),
      color: "text-tennis-lime",
    },
    {
      value: String(summary?.lessonsThisWeekCount ?? 0),
      label: t("statThisWeek"),
    },
    {
      value: `${summary?.totalEnrollmentCount ?? 0}`,
      label: t("statEnrollments"),
    },
    {
      value: String(summary?.activeTrainerCount ?? 0),
      label: t("statTrainers"),
    },
    {
      value: String(summary?.activeSeriesCount ?? 0),
      label: t("statSeries"),
    },
  ];

  const lessonsData = metrics?.lessons?.map((m) => m.value) ?? [];
  const occupancyData = metrics?.occupancyPct?.map((m) => m.value) ?? [];
  const outstandingData = metrics?.outstandingCents?.map((m) => m.value) ?? [];

  const lastLessons = lessonsData.length > 0 ? lessonsData[lessonsData.length - 1] : 0;
  const lastOccupancy = occupancyData.length > 0 ? occupancyData[occupancyData.length - 1] : 0;
  const lastOutstandingCents = outstandingData.length > 0 ? outstandingData[outstandingData.length - 1] : 0;
  const lastOutstandingEur = (lastOutstandingCents / 100).toFixed(0);

  const hasInboxItems = inbox?.items && inbox.items.length > 0;

  return (
    <>
      {/* Page header */}
      <div className="flex items-center justify-between mb-3.5">
        <div>
          <h1 className="text-lg font-bold text-ink tracking-tight">
            {t("title")}
          </h1>
        </div>
        <Link
          href="/dashboard/lessons/new"
          className="inline-flex items-center gap-1.5 px-3 py-[7px] bg-ink text-white text-xs font-semibold rounded-md"
        >
          <Plus size={12} />
          {t("newLesson")}
        </Link>
      </div>

      {/* Dark hero stat strip */}
      {isLoading ? (
        <div className="bg-ink rounded-xl p-4 mb-3.5 animate-pulse h-[72px]" />
      ) : (
        <StatStrip items={statItems} className="mb-3.5" />
      )}

      {/* Priority row — inbox items */}
      <InboxSection inbox={inbox} />

      {/* Two-column: today schedule + sparklines */}
      <div className="grid grid-cols-1 xl:grid-cols-[1.6fr_1fr] gap-[18px]">
        {/* Today schedule */}
        <div className="bg-paper border border-rule rounded-xl overflow-hidden">
          <div className="px-[18px] py-3.5 border-b border-rule flex items-center justify-between">
            <h2 className="text-[13px] font-bold text-ink">
              {t("todayTitle", { count: summary?.upcomingLessons?.length ?? 0 })}
            </h2>
          </div>

          {isLoading && (
            <div className="p-6 space-y-3 animate-pulse">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-12 bg-canvas rounded-lg" />
              ))}
            </div>
          )}

          {!isLoading && (!summary?.upcomingLessons || summary.upcomingLessons.length === 0) && (
            <div className="flex flex-col items-center justify-center py-14 px-6 text-center">
              <div className="w-14 h-14 rounded-full bg-tennis-green/[.08] flex items-center justify-center mb-4">
                <TennisBallEmptyIcon className="w-7 h-7" />
              </div>
              <p className="text-ink font-semibold text-sm mb-1">
                {t("noLessonsTitle")}
              </p>
              <p className="text-ink-3 text-xs max-w-60 leading-relaxed mb-5">
                {t("noLessonsDesc")}
              </p>
              <Link
                href="/dashboard/lessons/new"
                className="inline-flex items-center gap-2 px-4 py-2.5 bg-ink text-white text-xs font-semibold rounded-lg"
              >
                <Plus size={13} />
                {t("planLesson")}
              </Link>
            </div>
          )}

          {!isLoading && summary?.upcomingLessons && summary.upcomingLessons.length > 0 && (
            <div className="divide-y divide-rule">
              {summary.upcomingLessons.map((lesson) => (
                <div key={lesson.id} className="px-[18px] py-3.5 flex items-center justify-between">
                  <div className="flex items-center gap-3.5">
                    <div className="w-10 h-10 rounded-lg bg-tennis-green/10 flex items-center justify-center">
                      <Calendar size={16} className="text-tennis-green" />
                    </div>
                    <div>
                      <p className="text-[12.5px] font-semibold text-ink">
                        {lesson.seriesName}
                      </p>
                      <p className="text-[11px] text-ink-3">
                        {lesson.trainerName} · {lesson.courtName}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <Mono className="text-[12px] font-medium text-ink">
                      {formatDate(lesson.date)}
                    </Mono>
                    <p className="text-[11px] text-ink-3 flex items-center gap-1 justify-end">
                      <Clock size={10} />
                      <Mono>{formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}</Mono>
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Week sparklines */}
        <div className="flex flex-col gap-2.5">
          {[
            {
              label: t("sparkLessons"),
              val: lessonsData.length > 0 ? String(lastLessons) : String(summary?.lessonsThisWeekCount ?? 0),
              sub: t("sparkLessonsVsPrev"),
              data: lessonsData.length >= 2 ? lessonsData : [24, 28, 30, 26, 32, 35, summary?.lessonsThisWeekCount ?? 38],
              color: "#2D5016",
            },
            {
              label: t("sparkOccupancy"),
              val: occupancyData.length > 0 ? `${lastOccupancy}%` : "\u2014",
              sub: t("sparkOccupancyPct"),
              data: occupancyData.length >= 2 ? occupancyData : [60, 65, 70, 72, 75, 78, 80],
              color: "#2D5016",
            },
            {
              label: t("sparkOutstanding"),
              val: outstandingData.length > 0 ? `\u20AC${lastOutstandingEur}` : "\u2014",
              sub: t("sparkOutstandingLabel"),
              data: outstandingData.length >= 2 ? outstandingData : [0],
              color: "#2D5016",
            },
          ].map((s, i) => (
            <div
              key={i}
              className="bg-paper border border-rule rounded-xl p-3.5 flex items-center gap-3.5"
            >
              <div className="flex-1">
                <p className="text-[10.5px] text-ink-3 font-medium uppercase tracking-[0.05em]">
                  {s.label}
                </p>
                <p className="text-[22px] font-bold text-ink tracking-tight mt-1 tabular-nums">
                  {s.val}
                </p>
                <p className="text-[10.5px] text-ink-3 mt-0.5">{s.sub}</p>
              </div>
              <Sparkline data={s.data} color={s.color} />
            </div>
          ))}
        </div>
      </div>
    </>
  );
}
