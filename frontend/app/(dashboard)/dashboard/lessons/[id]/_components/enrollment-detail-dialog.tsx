"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { X } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { enrollmentStatusStyles } from "@/lib/status-styles";
import { getEnrollmentsWithPreferences } from "@/lib/api/enrollments";
import type { LessonSeriesEnrollmentDto } from "@/lib/api/enrollments";
import { getPublicTimeSlots } from "@/lib/api/timeSlots";
import type { TimeSlotDto } from "@/lib/api/timeSlots";

const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

const PREF_AVAILABLE = 1;
const PREF_PREFERRED = 2;
const PREF_UNAVAILABLE = 3;

function computeAge(dob: string | null): number | null {
  if (!dob) return null;
  const b = new Date(dob + "T00:00:00");
  if (Number.isNaN(b.getTime())) return null;
  const now = new Date();
  let age = now.getFullYear() - b.getFullYear();
  const m = now.getMonth() - b.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < b.getDate())) age--;
  return age;
}

export function EnrollmentDetailDialog({
  enrollment,
  seriesId,
  open,
  onOpenChange,
  onEdit,
  leaderName,
  groupMembers,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onEdit: () => void;
  leaderName?: string | null;
  /** Wanneer gezet: toon een 'Leden'-sectie (groep-detail). `enrollment` = de leider. */
  groupMembers?: LessonSeriesEnrollmentDto[];
}) {
  const t = useTranslations("enrollmentDetail");

  const { data: prefsData, isLoading: prefsLoading } = useQuery({
    queryKey: ["enrollmentPrefs", seriesId],
    queryFn: () => getEnrollmentsWithPreferences(seriesId),
    enabled: open,
  });

  const { data: timeSlots = [], isLoading: slotsLoading } = useQuery({
    queryKey: ["publicTimeSlots", seriesId],
    queryFn: () => getPublicTimeSlots(seriesId),
    enabled: open,
  });

  // Voorkeuren van déze inschrijving, geïndexeerd op slot-id.
  const prefMap = useMemo(() => {
    const map = new Map<string, number>();
    const mine = prefsData?.find((e) => e.id === enrollment.id);
    for (const p of mine?.preferences ?? []) map.set(p.weeklyTemplateEntryId, p.preference);
    return map;
  }, [prefsData, enrollment.id]);

  const isMember = enrollment.enrollmentGroupId != null && !enrollment.isGroupLeader;
  const age = computeAge(enrollment.dateOfBirth);
  const contact = enrollment.hasOwnEmail
    ? (enrollment.studentEmail ?? "")
    : t("viaContact", { email: enrollment.contactEmail });

  const roleBadge =
    enrollment.enrollmentGroupId == null
      ? t("solo")
      : enrollment.isGroupLeader
        ? t("leader")
        : t("memberOf", { group: leaderName ? `Groep · ${leaderName}` : t("solo") });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onClick={(e) => e.stopPropagation()}
        className="sm:max-w-lg max-h-[85vh] overflow-y-auto"
      >
        <DialogHeader>
          <DialogTitle className="flex flex-wrap items-center gap-2">
            {enrollment.studentName}
            <span className="rounded bg-gray-100 px-1.5 py-0.5 text-[11px] font-medium text-gray-600">
              {roleBadge}
            </span>
            {enrollmentStatusStyles[enrollment.status] && (
              <Badge
                className={`${enrollmentStatusStyles[enrollment.status].className} border-0 text-xs`}
              >
                {enrollmentStatusStyles[enrollment.status].label}
              </Badge>
            )}
          </DialogTitle>
        </DialogHeader>

        {/* Basisgegevens */}
        <section className="space-y-2">
          <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
            {t("sectionBasic")}
          </h3>
          <dl className="grid grid-cols-[130px_1fr] gap-x-3 gap-y-1.5 text-sm">
            <DetailRow label={t("contact")} value={contact} />
            {enrollment.studentPhone && (
              <DetailRow label="Telefoon" value={enrollment.studentPhone} />
            )}
            <DetailRow
              label={t("birthDate")}
              value={
                enrollment.dateOfBirth
                  ? `${new Date(enrollment.dateOfBirth + "T00:00:00").toLocaleDateString("nl-BE")}${
                      age != null ? ` (${t("ageYears", { count: age })})` : ""
                    }`
                  : t("unknown")
              }
            />
            {enrollment.categoryLabel && (
              <DetailRow label={t("category")} value={enrollment.categoryLabel} />
            )}
            <DetailRow
              label={t("enrolledAt")}
              value={new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
            />
            <DetailRow
              label={t("openToGrouping")}
              value={enrollment.isOpenToGrouping ? t("yes") : t("no")}
            />
          </dl>
        </section>

        {/* Leden (groep-detail) */}
        {groupMembers && groupMembers.length > 0 && (
          <section className="space-y-2">
            <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
              {t("sectionMembers")}
            </h3>
            <ul className="divide-y divide-gray-50 rounded-lg border border-gray-100">
              {groupMembers.map((m) => {
                const mAge = computeAge(m.dateOfBirth);
                const mContact = m.hasOwnEmail
                  ? (m.studentEmail ?? "")
                  : t("viaContact", { email: m.contactEmail });
                return (
                  <li
                    key={m.id}
                    className="flex items-center justify-between gap-3 px-3 py-2 text-sm"
                  >
                    <span className="flex min-w-0 items-center gap-2">
                      <span className="font-medium text-gray-800">{m.studentName}</span>
                      {m.isGroupLeader && (
                        <span className="shrink-0 rounded bg-tennis-green/10 px-1.5 py-0.5 text-[10px] font-semibold text-tennis-green">
                          {t("leaderBadge")}
                        </span>
                      )}
                    </span>
                    <span className="max-w-[55%] shrink-0 truncate text-right text-xs text-gray-500">
                      {mContact}
                      {mAge != null ? ` · ${t("ageYears", { count: mAge })}` : ""}
                    </span>
                  </li>
                );
              })}
            </ul>
          </section>
        )}

        {/* Formulierantwoorden */}
        <section className="space-y-2">
          <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
            {t("sectionResponses")}
          </h3>
          {enrollment.formResponses.length === 0 ? (
            <p className="text-sm text-gray-400">{t("noResponses")}</p>
          ) : (
            <dl className="space-y-1.5">
              {enrollment.formResponses.map((r, i) => (
                <div key={i} className="grid grid-cols-[130px_1fr] gap-x-3 text-sm">
                  <dt className="text-gray-500">{r.fieldLabel}</dt>
                  <dd className="font-medium text-gray-800">{r.value}</dd>
                </div>
              ))}
            </dl>
          )}
        </section>

        {/* Beschikbaarheden */}
        <section className="space-y-2">
          <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
            {t("sectionAvailability")}
          </h3>
          {slotsLoading || prefsLoading ? (
            <p className="text-sm text-gray-400">{t("loading")}</p>
          ) : prefMap.size === 0 ? (
            <p className="text-sm text-gray-400">
              {isMember ? t("availabilityViaLeader") : t("noAvailability")}
            </p>
          ) : (
            <AvailabilityGrid timeSlots={timeSlots} prefMap={prefMap} t={t} />
          )}
        </section>

        {/* Footer */}
        <div className="mt-2 flex justify-end gap-2 border-t border-gray-100 pt-4">
          <button
            type="button"
            onClick={() => onOpenChange(false)}
            className="rounded-lg border border-gray-200 px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
          >
            {t("close")}
          </button>
          {!groupMembers && (
            <button
              type="button"
              onClick={onEdit}
              className="rounded-lg bg-tennis-green px-3 py-2 text-sm font-medium text-white hover:bg-tennis-green/90"
            >
              {t("edit")}
            </button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <>
      <dt className="text-gray-500">{label}</dt>
      <dd className="text-gray-800">{value}</dd>
    </>
  );
}

// ─── Availability grid (read-only) ──────────────────────────────────────────────

function PrefDot({ pref, title }: { pref: number | undefined; title?: string }) {
  if (pref === PREF_PREFERRED)
    return <span title={title} className="inline-block h-3 w-3 rounded-full bg-green-500" />;
  if (pref === PREF_AVAILABLE)
    return <span title={title} className="inline-block h-3 w-3 rounded-full border-2 border-blue-500" />;
  if (pref === PREF_UNAVAILABLE) return <X size={13} className="text-gray-400" />;
  return <span className="text-gray-300">–</span>;
}

function AvailabilityGrid({
  timeSlots,
  prefMap,
  t,
}: {
  timeSlots: TimeSlotDto[];
  prefMap: Map<string, number>;
  t: (key: string) => string;
}) {
  const { days, ranges, cells } = useMemo(() => {
    const daySet = new Set<number>();
    const rangeSet = new Set<string>();
    // range-key + day → lijst van slots (parallelle banen mogelijk)
    const cellMap = new Map<string, TimeSlotDto[]>();
    for (const s of timeSlots) {
      daySet.add(s.dayOfWeek);
      const range = `${s.startTime}–${s.endTime}`;
      rangeSet.add(range);
      const key = `${range}|${s.dayOfWeek}`;
      const list = cellMap.get(key) ?? [];
      list.push(s);
      cellMap.set(key, list);
    }
    return {
      days: [...daySet].sort((a, b) => a - b),
      ranges: [...rangeSet].sort(),
      cells: cellMap,
    };
  }, [timeSlots]);

  return (
    <div className="space-y-2">
      <div className="overflow-x-auto">
        <table className="border-collapse text-sm">
          <thead>
            <tr className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
              <th className="py-1 pr-3 text-left font-semibold" />
              {days.map((d) => (
                <th key={d} className="px-3 py-1 text-center font-semibold">
                  {DAY_NAMES_SHORT[d]}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {ranges.map((range) => (
              <tr key={range} className="border-t border-gray-100">
                <td className="whitespace-nowrap py-2 pr-3 text-xs text-gray-600">{range}</td>
                {days.map((d) => {
                  const slots = cells.get(`${range}|${d}`) ?? [];
                  return (
                    <td key={d} className="px-3 py-2 text-center">
                      {slots.length === 0 ? (
                        <span className="text-gray-200">·</span>
                      ) : (
                        <span className="inline-flex items-center justify-center gap-1">
                          {slots.map((s) => (
                            <PrefDot
                              key={s.id}
                              pref={prefMap.get(s.id)}
                              title={s.courtName}
                            />
                          ))}
                        </span>
                      )}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Legende */}
      <div className="flex flex-wrap items-center gap-4 text-xs text-gray-500">
        <span className="flex items-center gap-1.5">
          <span className="inline-block h-3 w-3 rounded-full bg-green-500" />
          {t("prefPreferred")}
        </span>
        <span className="flex items-center gap-1.5">
          <span className="inline-block h-3 w-3 rounded-full border-2 border-blue-500" />
          {t("prefAvailable")}
        </span>
        <span className="flex items-center gap-1.5">
          <X size={13} className="text-gray-400" />
          {t("prefUnavailable")}
        </span>
      </div>
    </div>
  );
}
