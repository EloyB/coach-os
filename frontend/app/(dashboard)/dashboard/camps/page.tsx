"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Plus } from "lucide-react";
import { getCamps, type CampDto } from "@/lib/api/camps";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { SlashLabel } from "@/components/ui/slash-label";
import { Mono } from "@/components/ui/mono";
import { OccupancyBar } from "@/components/ui/occupancy-bar";

function formatDateRange(start: string, end: string): string {
  const fmt = (d: string) => {
    const [, m, day] = d.split("-");
    const months = [
      "jan",
      "feb",
      "mrt",
      "apr",
      "mei",
      "jun",
      "jul",
      "aug",
      "sep",
      "okt",
      "nov",
      "dec",
    ];
    return `${day} ${months[parseInt(m, 10) - 1]}`;
  };
  return `${fmt(start)} → ${fmt(end)}`;
}

function EmptyState() {
  const t = useTranslations("camps");
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
        href="/dashboard/camps/new"
        className="inline-flex items-center gap-2 px-5 py-2.5 bg-ink text-white text-sm font-semibold rounded-lg"
      >
        <Plus size={14} />
        {t("create")}
      </Link>
    </div>
  );
}

function CampRow({ camp }: { camp: CampDto }) {
  const t = useTranslations("camps");
  const hasCapacity = camp.maxParticipants != null && camp.maxParticipants > 0;

  return (
    <Link
      href={`/dashboard/camps/${camp.id}`}
      className="grid grid-cols-[2.2fr_1.1fr_1.2fr_0.9fr_0.7fr] px-4 py-3.5 items-center border-b border-rule last:border-b-0 text-xs hover:bg-canvas/50 transition-colors"
    >
      <div>
        <p className="text-ink font-semibold text-[12px] m-0">{camp.name}</p>
        <Mono className="text-[10.5px] text-ink-3 mt-0.5 block">
          {t("listDays", { count: camp.dayCount })} · {camp.tennisClubName}
        </Mono>
      </div>
      <Mono className="text-ink-2 text-[11px]">
        {formatDateRange(camp.startDate, camp.endDate)}
      </Mono>
      {hasCapacity ? (
        <OccupancyBar
          filled={camp.participantCount}
          capacity={camp.maxParticipants ?? 0}
        />
      ) : (
        <Mono className="text-ink-2 text-[11px]">
          {t("occupancyValue", { count: camp.participantCount })}
        </Mono>
      )}
      <Mono className="text-ink font-bold">
        {camp.price > 0 ? `€${camp.price}` : t("priceFree")}
      </Mono>
      <div className="text-right">
        {camp.isActive ? (
          <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-tennis-green/10 text-tennis-green font-semibold">
            ● {t("statusActive")}
          </span>
        ) : (
          <span className="text-[10.5px] px-2 py-0.5 rounded-full bg-canvas text-ink-3 font-semibold">
            ○ {t("statusDraft")}
          </span>
        )}
      </div>
    </Link>
  );
}

export default function CampsPage() {
  const t = useTranslations("camps");

  const { data: camps, isLoading, isError } = useQuery({
    queryKey: ["camps"],
    queryFn: getCamps,
  });

  const activeCount = camps?.filter((c) => c.isActive).length ?? 0;
  const draftCount = camps ? camps.length - activeCount : 0;

  return (
    <>
      {/* Page header */}
      <div className="flex items-center justify-between mb-5">
        <div>
          <SlashLabel>
            {activeCount} {t("statusActive").toLowerCase()} · {draftCount}{" "}
            {t("statusDraft").toLowerCase()}
          </SlashLabel>
          <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">
            {t("title")}
          </h1>
        </div>
        <div className="flex items-center gap-2.5">
          <Link
            href="/dashboard/camps/new"
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

      {!isLoading && !isError && camps?.length === 0 && <EmptyState />}

      {!isLoading && !isError && camps && camps.length > 0 && (
        <div className="bg-paper border border-rule rounded-xl overflow-hidden">
          {/* Column header */}
          <div className="grid grid-cols-[2.2fr_1.1fr_1.2fr_0.9fr_0.7fr] px-4 py-2.5 text-[10.5px] text-ink-3 font-semibold font-mono uppercase tracking-[0.08em] border-b border-rule bg-[#fbfaf6]">
            <span>{t("title")}</span>
            <span>{t("listPeriod")}</span>
            <span>{t("listOccupancy")}</span>
            <span>{t("listPrice")}</span>
            <span className="text-right">{t("listStatus")}</span>
          </div>
          {camps.map((c) => (
            <CampRow key={c.id} camp={c} />
          ))}
        </div>
      )}
    </>
  );
}
