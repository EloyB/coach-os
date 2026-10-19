"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { format, parseISO } from "date-fns";
import { nl } from "date-fns/locale";
import { ArrowRight } from "lucide-react";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import { OccupancyBar } from "@/components/ui/occupancy-bar";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  getClubOverview,
  type ClubAttentionItemDto,
  type ClubOverviewDto,
  type ClubSeriesDto,
} from "@/lib/api/dashboard";
import { cn } from "@/lib/utils";

type ClubOverviewTab = "occupancy" | "attention";

function formatDate(iso: string) {
  return format(parseISO(iso), "EEE d MMM", { locale: nl });
}

/** Query voor het cluboverzicht; enkel actief voor hoofdtrainers. */
export function useClubOverview(enabled: boolean) {
  return useQuery({
    queryKey: ["dashboard", "club-overview"],
    queryFn: getClubOverview,
    enabled,
  });
}

// ─── Tegel op het dashboard ──────────────────────────────────────────────────

/** Compacte tegel; een klik opent het volledige cluboverzicht in een dialog. */
export function ClubOverviewCard({
  overview,
  onOpen,
}: {
  overview: ClubOverviewDto | undefined;
  onOpen: () => void;
}) {
  const t = useTranslations("dashboard");
  const shell = "bg-paper border border-rule rounded-xl p-[18px]";

  if (!overview)
    return <div className={cn(shell, "h-[132px] animate-pulse")} />;

  const hasAttention = overview.attentionCount > 0;

  return (
    <button
      type="button"
      onClick={onOpen}
      aria-label={t("clubOverviewOpen")}
      className={cn(
        shell,
        "group cursor-pointer text-left transition-colors hover:border-ink/40 hover:bg-canvas/60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ink",
      )}
    >
      <p className="text-[10.5px] font-medium uppercase tracking-[0.05em] text-ink-3">
        {t("clubOverviewTitle")}
      </p>
      <p className="mt-1 text-[26px] font-bold leading-none tracking-tight text-ink tabular-nums">
        {overview.averageOccupancyPercent !== null
          ? t("clubOverviewOccupancy", {
              percent: overview.averageOccupancyPercent,
            })
          : overview.activeSeriesCount}
      </p>
      <p className="mt-1 text-[10.5px] text-ink-3">
        {t("clubOverviewSeries", { count: overview.activeSeriesCount })}
      </p>
      <span
        className={cn(
          "mt-3 flex items-center justify-between gap-2 rounded-lg px-2.5 py-1.5 text-[11.5px] font-semibold",
          hasAttention
            ? "bg-amber-50 text-amber-700"
            : "bg-tennis-green/10 text-tennis-green",
        )}
      >
        {hasAttention
          ? t("clubOverviewAttention", { count: overview.attentionCount })
          : t("clubOverviewViewOverview")}
        <ArrowRight
          size={14}
          aria-hidden
          className="shrink-0 transition-transform group-hover:translate-x-0.5"
        />
      </span>
    </button>
  );
}

// ─── Tabbladen ───────────────────────────────────────────────────────────────

function SeriesLink({
  seriesId,
  children,
}: {
  seriesId: string | null;
  children: React.ReactNode;
}) {
  if (!seriesId) return <>{children}</>;
  return (
    <Link
      href={`/dashboard/lessons/${seriesId}`}
      className="font-medium text-ink hover:underline"
    >
      {children}
    </Link>
  );
}

function OccupancyTab({ series }: { series: ClubSeriesDto[] }) {
  const t = useTranslations("dashboard");

  if (series.length === 0)
    return (
      <p className="rounded-lg border border-dashed border-rule px-3 py-6 text-center text-xs text-ink-3">
        {t("clubOverviewNoSeries")}
      </p>
    );

  return (
    <ul className="divide-y divide-rule rounded-lg border border-rule">
      {series.map((s) => (
        <li key={s.id} className="space-y-1.5 px-3 py-2.5 text-[13px]">
          <div className="flex items-baseline justify-between gap-3">
            <SeriesLink seriesId={s.id}>
              <span className="truncate">{s.name}</span>
            </SeriesLink>
            {s.tennisClubName && (
              <span className="shrink-0 text-[11px] text-ink-3">
                {s.tennisClubName}
              </span>
            )}
          </div>
          {s.maxRegistrations ? (
            <OccupancyBar filled={s.enrolled} capacity={s.maxRegistrations} />
          ) : (
            <Mono className="text-[11px] text-ink-3">
              {s.enrolled} · {t("clubOverviewNoMax")}
            </Mono>
          )}
          <p className="text-[11px] text-ink-3">
            {s.trainerNames.length > 0
              ? s.trainerNames.join(", ")
              : t("clubOverviewNoTrainer")}
          </p>
        </li>
      ))}
    </ul>
  );
}

function pendingRemaining(
  t: ReturnType<typeof useTranslations>,
  hours: number,
): { text: string; urgent: boolean } {
  if (hours < 0) return { text: t("clubOverviewPendingExpired"), urgent: true };
  if (hours >= 24)
    return {
      text: t("clubOverviewPendingLeftDays", {
        days: Math.floor(hours / 24),
        hours: hours % 24,
      }),
      urgent: false,
    };
  return {
    text: t("clubOverviewPendingLeftHours", { hours }),
    urgent: hours < 12,
  };
}

function AttentionRow({ item }: { item: ClubAttentionItemDto }) {
  const t = useTranslations("dashboard");
  const seriesName = item.seriesName ?? t("trainerStandaloneLesson");
  let title = "";
  let body = "";
  let meta: { text: string; urgent: boolean } | null = null;

  switch (item.type) {
    case "series_underbooked":
      title = t("clubOverviewUnderbooked");
      body = t("clubOverviewUnderbookedBody", {
        enrolled: item.enrolled ?? 0,
        max: item.maxRegistrations ?? 0,
      });
      break;
    case "confirmation_pending":
      title = t("clubOverviewPending");
      body = t("clubOverviewPendingBody", {
        count: item.studentNames.length,
        names: item.studentNames.slice(0, 3).join(", "),
        more: Math.max(0, item.studentNames.length - 3),
      });
      meta =
        item.hoursRemaining !== null
          ? pendingRemaining(t, item.hoursRemaining)
          : null;
      break;
    case "planning_stalled":
      title = t("clubOverviewStalled");
      body = t("clubOverviewStalledBody", {
        date: item.date ? formatDate(item.date) : "",
      });
      break;
    case "lesson_without_trainer":
      title = t("clubOverviewNoTrainerLesson");
      body = t("clubOverviewNoTrainerLessonBody", {
        date: item.date ? formatDate(item.date) : "",
      });
      break;
  }

  return (
    <li className="space-y-1.5 rounded-lg border border-rule bg-paper px-3.5 py-3 text-[13px]">
      <div className="flex items-center justify-between gap-3">
        <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10.5px] font-semibold text-amber-800">
          {title}
        </span>
        {meta && (
          <span
            className={cn(
              "shrink-0 text-[11px]",
              meta.urgent ? "font-semibold text-red-600" : "text-ink-3",
            )}
          >
            {meta.text}
          </span>
        )}
      </div>
      <p className="text-ink">
        <SeriesLink seriesId={item.seriesId}>{seriesName}</SeriesLink>
        {item.tennisClubName && (
          <span className="text-ink-3"> · {item.tennisClubName}</span>
        )}
      </p>
      <p className="text-[12px] text-ink-3">{body}</p>
    </li>
  );
}

function AttentionTab({ items }: { items: ClubAttentionItemDto[] }) {
  const t = useTranslations("dashboard");

  if (items.length === 0)
    return (
      <p className="rounded-lg border border-dashed border-rule px-3 py-6 text-center text-xs text-ink-3">
        {t("clubOverviewNoAttention")}
      </p>
    );

  return (
    <ul className="space-y-2.5">
      {items.map((item, i) => (
        <AttentionRow key={`${item.type}-${item.seriesId}-${i}`} item={item} />
      ))}
    </ul>
  );
}

// ─── Dialog ──────────────────────────────────────────────────────────────────

export function ClubOverviewDialog({
  overview,
  isError,
  open,
  onClose,
}: {
  overview: ClubOverviewDto | undefined;
  isError: boolean;
  open: boolean;
  onClose: () => void;
}) {
  const t = useTranslations("dashboard");
  const [tab, setTab] = useState<ClubOverviewTab>("attention");

  const tabButton = (value: ClubOverviewTab, label: string, count?: number) => (
    <button
      type="button"
      role="tab"
      aria-selected={tab === value}
      onClick={() => setTab(value)}
      className={cn(
        "rounded-md px-3 py-1 text-[11.5px] font-semibold transition-colors",
        tab === value
          ? "bg-ink text-white"
          : "text-ink-3 hover:text-ink hover:bg-canvas",
      )}
    >
      {label}
      {count ? <span className="ml-1.5 opacity-70">{count}</span> : null}
    </button>
  );

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t("clubOverviewDialogTitle")}</DialogTitle>
          <DialogDescription>
            {t("clubOverviewDialogDescription")}
          </DialogDescription>
        </DialogHeader>

        <div
          role="tablist"
          aria-label={t("clubOverviewTabsAria")}
          className="inline-flex gap-0.5 self-start rounded-lg border border-rule bg-paper p-0.5"
        >
          {tabButton(
            "attention",
            t("clubOverviewTabAttention"),
            overview?.attentionCount,
          )}
          {tabButton("occupancy", t("clubOverviewTabOccupancy"))}
        </div>

        {isError && (
          <p className="text-xs text-red-600">{t("clubOverviewLoadError")}</p>
        )}
        {!overview && !isError && (
          <div className="space-y-3 animate-pulse">
            <div className="h-24 rounded-lg bg-canvas" />
            <div className="h-20 rounded-lg bg-canvas" />
          </div>
        )}
        {overview && tab === "attention" && (
          <AttentionTab items={overview.attention} />
        )}
        {overview && tab === "occupancy" && (
          <OccupancyTab series={overview.series} />
        )}
      </DialogContent>
    </Dialog>
  );
}
