"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Pencil, UserMinus, X } from "lucide-react";
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
import { getLessonSeriePrices } from "@/lib/api/lessonSeriePrices";
import { getPublicTimeSlots } from "@/lib/api/timeSlots";
import { canEditEnrollment, isHeadTrainerViewer } from "@/lib/auth";
import type { TimeSlotDto } from "@/lib/api/timeSlots";

const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

const PREF_AVAILABLE = 1;
const PREF_PREFERRED = 2;
const PREF_UNAVAILABLE = 3;

type TabId = "gegevens" | "leden" | "beschikbaarheden";

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
  groupMembers,
  onEditMember,
  onRemoveMember,
  onChangeGroupPriceOption,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onEdit: () => void;
  /** Wanneer gezet: toon een 'Leden'-sectie (groep-detail). `enrollment` = de leider. */
  groupMembers?: LessonSeriesEnrollmentDto[];
  /** Wanneer gezet: toon per lid een bewerk-knop die dit lid opent. */
  onEditMember?: (member: LessonSeriesEnrollmentDto) => void;
  /** Wanneer gezet: toon per lid een 'uit groep halen'-knop. */
  onRemoveMember?: (member: LessonSeriesEnrollmentDto) => void;
  /** Wanneer gezet (bij een groep): toon een prijsoptie-selector voor de hele groep. */
  onChangeGroupPriceOption?: (optionId: string | null) => void;
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

  const { data: priceOptions = [] } = useQuery({
    queryKey: ["lessonSeriePrices", seriesId],
    queryFn: () => getLessonSeriePrices(seriesId),
    enabled: open && onChangeGroupPriceOption != null,
  });

  // De prijsoptie is één gedeelde waarde voor de groep (de leider = `enrollment` draagt de
  // betaling). Vergrendeld zodra de groep betaald/bevestigd/geannuleerd is — gelijk aan de gate.
  const groupPriceLocked =
    enrollment.status === "Confirmed" ||
    enrollment.status === "PendingPayment" ||
    enrollment.status === "Cancelled";

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

  const hasGroup = !!(groupMembers && groupMembers.length > 0);
  const tabs: { id: TabId; label: string }[] = [
    { id: "gegevens", label: t("sectionBasic") },
    ...(hasGroup ? [{ id: "leden" as TabId, label: t("tabMembers") }] : []),
    { id: "beschikbaarheden", label: t("sectionAvailability") },
  ];
  const [activeTab, setActiveTab] = useState<TabId>("gegevens");
  // Hoofdtrainer = read-only: geen bewerk-affordances in de detail-dialog.
  const [readOnly, setReadOnly] = useState(false);
  // Inschrijving bewerken is Admin-only (endpoint = RequireRole("Admin")); gewone
  // trainers zien de bewerk-knop dus niet i.p.v. een gegarandeerde 403 bij opslaan.
  const [canEdit, setCanEdit] = useState(false);
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setReadOnly(isHeadTrainerViewer());
    setCanEdit(canEditEnrollment());
  }, []);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onClick={(e) => e.stopPropagation()}
        className="flex max-h-[85vh] flex-col gap-0 overflow-hidden p-0 sm:max-w-xl"
      >
        {/* Scrollbaar deel */}
        <div className="flex flex-col gap-4 overflow-y-auto px-6 pt-6 pb-2">
        <DialogHeader>
          <DialogTitle className="flex flex-wrap items-center gap-2">
            {hasGroup ? t("groupTitle", { name: enrollment.studentName }) : enrollment.studentName}
            {enrollmentStatusStyles[enrollment.status] && (
              <Badge
                className={`${enrollmentStatusStyles[enrollment.status].className} border-0 text-xs`}
              >
                {enrollmentStatusStyles[enrollment.status].label}
              </Badge>
            )}
          </DialogTitle>
        </DialogHeader>

        {/* Tab-balk */}
        <div role="tablist" className="flex gap-1 border-b border-gray-100">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={activeTab === tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
                activeTab === tab.id
                  ? "border-tennis-green text-tennis-green"
                  : "border-transparent text-gray-500 hover:text-gray-700"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Tab: Gegevens (basis + formulierantwoorden) */}
        {activeTab === "gegevens" && (
          <div className="space-y-5">
            <dl className="space-y-3.5">
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

            <div className="space-y-2">
              <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
                {t("sectionResponses")}
              </h3>
              {enrollment.formResponses.length === 0 ? (
                <p className="text-sm text-gray-400">{t("noResponses")}</p>
              ) : (
                <dl className="space-y-3">
                  {enrollment.formResponses.map((r, i) => (
                    <div key={i}>
                      <dt className="text-xs text-gray-500">{r.fieldLabel}</dt>
                      <dd className="mt-0.5 text-sm text-gray-800">{r.value}</dd>
                    </div>
                  ))}
                </dl>
              )}
            </div>
          </div>
        )}

        {/* Tab: Groepsleden */}
        {activeTab === "leden" && hasGroup && (
          <div className="space-y-4">
            {onChangeGroupPriceOption && priceOptions.length > 0 && (
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">
                  {t("groupPriceLabel")}
                </label>
                {groupPriceLocked ? (
                  <p className="rounded-md border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">
                    {priceOptions.find((o) => o.id === enrollment.selectedPriceOptionId)?.label
                      ?? t("groupPriceNone")}
                    <span className="mt-1 block text-xs text-gray-400">{t("groupPriceLocked")}</span>
                  </p>
                ) : (
                  <select
                    value={enrollment.selectedPriceOptionId ?? ""}
                    onChange={(e) => onChangeGroupPriceOption(e.target.value || null)}
                    className="w-full rounded-md border border-gray-200 px-3 py-2 text-sm focus:border-tennis-green focus:outline-none"
                  >
                    <option value="">{t("groupPriceNone")}</option>
                    {priceOptions
                      .slice()
                      .sort((a, b) => a.sortOrder - b.sortOrder)
                      .map((o) => (
                        <option key={o.id} value={o.id}>
                          {o.label} — €{o.totalPrice}
                        </option>
                      ))}
                  </select>
                )}
                <p className="mt-1 text-xs text-gray-400">{t("groupPriceHint")}</p>
              </div>
            )}

            <ul className="divide-y divide-gray-50 rounded-lg border border-gray-100">
            {groupMembers!.map((m) => {
              const mAge = computeAge(m.dateOfBirth);
              const mContact = m.hasOwnEmail
                ? (m.studentEmail ?? "")
                : t("viaContact", { email: m.contactEmail });
              return (
                <li
                  key={m.id}
                  className="flex items-start gap-3 px-3 py-2.5 text-sm"
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="truncate font-medium text-gray-800">
                        {m.studentName}
                      </span>
                      {m.isGroupLeader && (
                        <span className="shrink-0 rounded bg-tennis-green/10 px-1.5 py-0.5 text-[10px] font-semibold text-tennis-green">
                          {t("leaderBadge")}
                        </span>
                      )}
                      {enrollmentStatusStyles[m.status] && (
                        <Badge
                          className={`${enrollmentStatusStyles[m.status].className} shrink-0 border-0 text-[10px]`}
                        >
                          {enrollmentStatusStyles[m.status].label}
                        </Badge>
                      )}
                    </div>
                    <div className="mt-0.5 truncate text-xs text-gray-500">
                      {[
                        mContact,
                        mAge != null ? t("ageYears", { count: mAge }) : null,
                        m.categoryLabel || null,
                      ]
                        .filter(Boolean)
                        .join(" · ")}
                    </div>
                  </div>
                  {((canEdit && onEditMember) || (!readOnly && onRemoveMember)) && (
                    <div className="mt-0.5 flex shrink-0 items-center gap-0.5">
                      {canEdit && onEditMember && (
                        <button
                          type="button"
                          onClick={() => onEditMember(m)}
                          aria-label={t("editMemberLabel", { name: m.studentName })}
                          className="flex h-7 w-7 items-center justify-center rounded-md text-gray-400 hover:bg-gray-50 hover:text-tennis-green"
                        >
                          <Pencil size={13} />
                        </button>
                      )}
                      {!readOnly && onRemoveMember && m.status !== "Cancelled" && (
                        <button
                          type="button"
                          onClick={() => onRemoveMember(m)}
                          aria-label={t("removeMemberLabel", { name: m.studentName })}
                          title={t("removeMemberLabel", { name: m.studentName })}
                          className="flex h-7 w-7 items-center justify-center rounded-md text-gray-400 hover:bg-tennis-green/5 hover:text-tennis-green"
                        >
                          <UserMinus size={13} />
                        </button>
                      )}
                    </div>
                  )}
                </li>
              );
            })}
            </ul>
          </div>
        )}

        {/* Tab: Beschikbaarheden */}
        {activeTab === "beschikbaarheden" &&
          (slotsLoading || prefsLoading ? (
            <p className="text-sm text-gray-400">{t("loading")}</p>
          ) : prefMap.size === 0 ? (
            <p className="text-sm text-gray-400">
              {isMember ? t("availabilityViaLeader") : t("noAvailability")}
            </p>
          ) : (
            <AvailabilityGrid timeSlots={timeSlots} prefMap={prefMap} t={t} />
          ))}
        </div>

        {/* Footer (vast onderaan; scrollt niet mee) */}
        <div className="flex shrink-0 justify-end gap-2 border-t border-gray-100 bg-background px-6 py-4">
          <button
            type="button"
            onClick={() => onOpenChange(false)}
            className="rounded-lg border border-gray-200 px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
          >
            {t("close")}
          </button>
          {canEdit && !groupMembers && (
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
    <div>
      <dt className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
        {label}
      </dt>
      <dd className="mt-0.5 text-sm text-gray-800">{value}</dd>
    </div>
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
    <div className="space-y-3">
      <div className="overflow-x-auto">
        <table className="w-full table-fixed border-collapse text-sm">
          <colgroup>
            <col style={{ width: "6.5rem" }} />
            {days.map((d) => (
              <col key={d} />
            ))}
          </colgroup>
          <thead>
            <tr className="text-[11px] font-semibold uppercase tracking-wide text-gray-400">
              <th className="py-2 pr-3 text-left font-semibold" />
              {days.map((d) => (
                <th key={d} className="px-2 py-2 text-center font-semibold">
                  {DAY_NAMES_SHORT[d]}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {ranges.map((range) => (
              <tr key={range} className="border-t border-gray-100">
                <td className="whitespace-nowrap py-3.5 pr-3 text-xs text-gray-600">
                  {range}
                </td>
                {days.map((d) => {
                  const slots = cells.get(`${range}|${d}`) ?? [];
                  return (
                    <td key={d} className="px-2 py-3.5 text-center">
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
