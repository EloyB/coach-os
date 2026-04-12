"use client";

import { use, useState, useMemo, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  ArrowLeft,
  RefreshCw,
  Check,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  getPlanningOverview,
  generatePlanning,
  confirmPlanning,
} from "@/lib/api/planning";
import type {
  PlanningOverviewDto,
  PlanningTimeSlotDto,
  PlanningEnrollmentDto,
  PlanningAssignmentDto,
  PlanningGroupDto,
} from "@/lib/api/planning";
import { getLessonSeriesById } from "@/lib/api/lessonSeries";
import {
  CalendarGrid,
  parseTime,
  getSlotPosition,
  layoutDaySlots,
  type CalendarSlot,
} from "@/components/calendar/calendar-grid";

// ─── Constants ───────────────────────────────────────────────────────────────

const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

const AVATAR_COLORS = [
  { bg: "bg-tennis-green", text: "text-white" },
  { bg: "bg-blue-100", text: "text-blue-700" },
  { bg: "bg-purple-100", text: "text-purple-700" },
  { bg: "bg-orange-100", text: "text-orange-700" },
  { bg: "bg-pink-100", text: "text-pink-700" },
  { bg: "bg-teal-100", text: "text-teal-700" },
  { bg: "bg-indigo-100", text: "text-indigo-700" },
  { bg: "bg-emerald-100", text: "text-emerald-700" },
];

function hashStr(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) h = ((h << 5) - h + s.charCodeAt(i)) | 0;
  return Math.abs(h);
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2)
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  return name.slice(0, 2).toUpperCase();
}

function getAvatarColor(name: string) {
  return AVATAR_COLORS[hashStr(name) % AVATAR_COLORS.length];
}

// ─── Page ────────────────────────────────────────────────────────────────────

export default function PlanningPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("planning");
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: series } = useQuery({
    queryKey: ["lessonSeries", id],
    queryFn: () => getLessonSeriesById(id),
  });

  const {
    data: planning,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["planning", id],
    queryFn: () => getPlanningOverview(id),
  });

  const generateMutation = useMutation({
    mutationFn: () => generatePlanning(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
    },
  });

  const confirmMutation = useMutation({
    mutationFn: () => confirmPlanning(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", id] });
      router.push(`/dashboard/lessons/${id}`);
    },
  });

  // Generate planning on first load
  const [hasGenerated, setHasGenerated] = useState(false);
  useEffect(() => {
    if (!hasGenerated && !isLoading) {
      setHasGenerated(true);
      generateMutation.mutate();
    }
  }, [isLoading]);

  // ─── Derived data ───────────────────────────────────────────────────────

  // Lookup maps
  const enrollmentMap = useMemo(() => {
    const map = new Map<string, PlanningEnrollmentDto>();
    if (!planning) return map;
    for (const e of planning.enrollments) map.set(e.id, e);
    return map;
  }, [planning]);

  const groupMap = useMemo(() => {
    const map = new Map<string, PlanningGroupDto>();
    if (!planning) return map;
    for (const g of planning.groups) map.set(g.id, g);
    return map;
  }, [planning]);

  // Assignments by timeSlotId
  const assignmentsBySlot = useMemo(() => {
    const map = new Map<string, PlanningAssignmentDto[]>();
    if (!planning) return map;
    for (const a of planning.assignments) {
      const list = map.get(a.timeSlotId) ?? [];
      list.push(a);
      map.set(a.timeSlotId, list);
    }
    return map;
  }, [planning]);

  // Enrolled IDs that have an assignment
  const assignedEnrollmentIds = useMemo(() => {
    const set = new Set<string>();
    if (!planning) return set;
    for (const a of planning.assignments) {
      if (a.enrollmentId) {
        set.add(a.enrollmentId);
      } else if (a.groupId) {
        const group = groupMap.get(a.groupId);
        if (group) group.memberEnrollmentIds.forEach((id) => set.add(id));
      }
    }
    return set;
  }, [planning, groupMap]);

  // Unassigned enrollments
  const unassigned = useMemo(() => {
    if (!planning) return [];
    return planning.enrollments.filter((e) => !assignedEnrollmentIds.has(e.id));
  }, [planning, assignedEnrollmentIds]);

  // Dynamic calendar hour range
  const { calStartHour, calEndHour } = useMemo(() => {
    if (!planning || planning.timeSlots.length === 0) {
      return { calStartHour: undefined, calEndHour: undefined };
    }
    let minMin = Infinity;
    let maxMin = -Infinity;
    for (const slot of planning.timeSlots) {
      const start = parseTime(slot.startTime);
      const end = parseTime(slot.endTime);
      if (start < minMin) minMin = start;
      if (end > maxMin) maxMin = end;
    }
    return {
      calStartHour: Math.max(0, Math.floor(minMin / 60) - 1) + 0.5,
      calEndHour: Math.min(24, Math.ceil(maxMin / 60) + 1),
    };
  }, [planning]);

  // Stats
  const totalAssigned = assignedEnrollmentIds.size;
  const totalUnassigned = unassigned.length;
  const totalSlots = planning?.timeSlots.length ?? 0;
  const totalCapacity =
    planning?.timeSlots.reduce((sum, s) => sum + s.maxCapacity, 0) ?? 0;
  const totalEnrollments = planning?.enrollments.length ?? 0;

  // Helper: get names for a slot's assignments
  function getSlotNames(slotId: string): string[] {
    const assignments = assignmentsBySlot.get(slotId) ?? [];
    const names: string[] = [];
    for (const a of assignments) {
      if (a.enrollmentId) {
        const e = enrollmentMap.get(a.enrollmentId);
        if (e) names.push(e.studentName);
      } else if (a.groupId) {
        const g = groupMap.get(a.groupId);
        if (g) {
          for (const memberId of g.memberEnrollmentIds) {
            const e = enrollmentMap.get(memberId);
            if (e) names.push(e.studentName);
          }
        }
      }
    }
    return names;
  }

  function getSlotCurrentCount(slotId: string): number {
    return getSlotNames(slotId).length;
  }

  function slotHasProposed(slotId: string): boolean {
    const assignments = assignmentsBySlot.get(slotId) ?? [];
    return assignments.some((a) => a.status === "Proposed");
  }

  // Helper: find which slot a group is assigned to
  function getGroupSlotLabel(groupId: string): string | null {
    if (!planning) return null;
    const assignment = planning.assignments.find((a) => a.groupId === groupId);
    if (!assignment) return null;
    const slot = planning.timeSlots.find((s) => s.id === assignment.timeSlotId);
    if (!slot) return null;
    return `${DAY_NAMES_SHORT[slot.dayOfWeek]} ${slot.startTime} — ${slot.endTime}`;
  }

  // ─── Loading / Error ────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <RefreshCw className="w-5 h-5 animate-spin text-gray-400" />
      </div>
    );
  }

  if (isError || !planning) {
    return (
      <div className="p-8">
        <p className="text-sm text-red-500">
          Fout bij het laden van de planning.
        </p>
      </div>
    );
  }

  // ─── Render ─────────────────────────────────────────────────────────────

  return (
    <div className="flex flex-col h-full -mx-8 -my-8">
      {/* Top bar */}
      <div className="bg-white border-b border-gray-200 px-8 py-4 flex items-center justify-between shrink-0">
        <div className="flex items-center gap-4">
          <Link
            href={`/dashboard/lessons/${id}`}
            className="text-sm text-gray-500 hover:text-tennis-green flex items-center gap-1"
          >
            <ArrowLeft size={16} />
            {t("backToSeries")}
          </Link>
          <div className="h-5 w-px bg-gray-200" />
          <h1 className="text-lg font-semibold text-gray-900">
            {t("pageTitle")} — {series?.name ?? "..."}
          </h1>
          {planning.planningStatus === "Planning" && (
            <span className="px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
              {t("pageTitle")}
            </span>
          )}
          {planning.planningStatus === "Scheduled" && (
            <span className="px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
              Bevestigd
            </span>
          )}
        </div>
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => generateMutation.mutate()}
            disabled={generateMutation.isPending}
            className="inline-flex items-center gap-2 border border-gray-300 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition disabled:opacity-50"
          >
            <RefreshCw
              size={16}
              className={generateMutation.isPending ? "animate-spin" : ""}
            />
            {generateMutation.isPending ? t("generating") : t("regenerate")}
          </button>

          <AlertDialog>
            <AlertDialogTrigger asChild>
              <button
                type="button"
                disabled={confirmMutation.isPending || totalUnassigned > 0}
                className="inline-flex items-center gap-2 bg-tennis-green text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-tennis-green/90 transition disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Check size={16} />
                {confirmMutation.isPending ? t("confirming") : t("confirm")}
              </button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t("confirmTitle")}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t("confirmDesc")}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Annuleren</AlertDialogCancel>
                <AlertDialogAction
                  onClick={() => confirmMutation.mutate()}
                  className="bg-tennis-green hover:bg-tennis-green/90"
                >
                  {t("confirmButton")}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </div>

      {/* Stats bar */}
      <div className="bg-white border-b border-gray-200 px-8 py-3 flex items-center gap-6 text-sm shrink-0">
        <div className="flex items-center gap-2">
          <div className="w-2.5 h-2.5 rounded-full bg-green-500" />
          <span className="text-gray-600">
            {t("assigned")}: <strong>{totalAssigned}</strong>
          </span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-2.5 h-2.5 rounded-full bg-red-500" />
          <span className="text-gray-600">
            {t("unassigned")}: <strong>{totalUnassigned}</strong>
          </span>
        </div>
        <div className="ml-auto text-xs text-gray-400">
          {totalEnrollments} {t("enrollments")} · {totalSlots} {t("timeSlots")}{" "}
          · {totalCapacity} {t("spots")}
        </div>
      </div>

      {/* Calendar + Sidebar */}
      <div className="flex-1 flex overflow-hidden">
        {/* Calendar area */}
        <div className="flex-1 p-6 overflow-auto">
          <CalendarGrid
            slots={[]}
            readOnly
            startHour={calStartHour}
            endHour={calEndHour}
            renderDayOverlay={(dayIndex) => {
              const daySlots = planning.timeSlots.filter(
                (s) => s.dayOfWeek === dayIndex
              );

              const asCalendarSlots: CalendarSlot[] = daySlots.map((s) => ({
                id: s.id,
                dayOfWeek: s.dayOfWeek,
                startTime: s.startTime,
                endTime: s.endTime,
                trainerId: s.trainerId ?? null,
              }));
              const layout = layoutDaySlots(asCalendarSlots);

              return (
                <>
                  {daySlots.map((slot) => {
                    const pos = getSlotPosition(
                      {
                        id: slot.id,
                        dayOfWeek: slot.dayOfWeek,
                        startTime: slot.startTime,
                        endTime: slot.endTime,
                        trainerId: null,
                      },
                      calStartHour
                    );
                    const col = layout.get(slot.id) ?? {
                      colIndex: 0,
                      totalCols: 1,
                    };
                    const colWidthPct = 100 / col.totalCols;
                    const names = getSlotNames(slot.id);
                    const currentCount = getSlotCurrentCount(slot.id);
                    const hasProposed = slotHasProposed(slot.id);
                    const borderColor = hasProposed
                      ? "border-amber-300"
                      : "border-green-300";
                    const bgColor = hasProposed ? "bg-amber-50" : "bg-green-50";
                    const isSingle = names.length === 1;

                    return (
                      <div
                        key={slot.id}
                        className={`absolute ${bgColor} border ${borderColor} rounded-lg px-2 py-1.5 cursor-pointer shadow-sm hover:shadow-md transition-shadow z-10 overflow-hidden`}
                        style={{
                          top: pos.top,
                          height: pos.height,
                          left: `calc(${col.colIndex * colWidthPct}% + 1px)`,
                          width: `calc(${colWidthPct}% - 2px)`,
                        }}
                      >
                        {/* Header: court + capacity */}
                        <div className="flex items-center justify-between">
                          <span className="text-[10px] font-medium text-gray-500 truncate">
                            {slot.courtName ?? ""}
                          </span>
                          <span
                            className={`text-[10px] shrink-0 ${
                              hasProposed
                                ? "text-amber-600"
                                : "text-green-600"
                            }`}
                          >
                            {currentCount}/{slot.maxCapacity}
                            {hasProposed && " ⚠"}
                          </span>
                        </div>

                        {/* Assigned people */}
                        {pos.height >= 36 &&
                          (() => {
                            if (names.length === 0) return null;

                            if (isSingle) {
                              const name = names[0];
                              const color = getAvatarColor(name);
                              const enrollment = planning.enrollments.find(
                                (e) => e.studentName === name
                              );
                              const pref = enrollment?.preferences[slot.id];
                              return (
                                <div className="mt-1 flex items-center gap-1">
                                  <div
                                    className={`w-4 h-4 rounded-full ${color.bg} ${color.text} flex items-center justify-center text-[8px] font-bold shrink-0`}
                                  >
                                    {getInitials(name)}
                                  </div>
                                  <span className="text-[10px] text-gray-700 truncate">
                                    {name}
                                  </span>
                                  {pref === "Preferred" && (
                                    <span className="text-[9px] text-green-600 ml-auto shrink-0">
                                      ★
                                    </span>
                                  )}
                                </div>
                              );
                            }

                            return (
                              <div className="mt-1 flex items-center gap-0.5 flex-wrap">
                                {names.map((name, i) => {
                                  const color = getAvatarColor(name);
                                  return (
                                    <div
                                      key={i}
                                      title={name}
                                      className={`w-5 h-5 rounded-full ${color.bg} ${color.text} flex items-center justify-center text-[8px] font-bold shrink-0`}
                                    >
                                      {getInitials(name)}
                                    </div>
                                  );
                                })}
                              </div>
                            );
                          })()}
                      </div>
                    );
                  })}
                </>
              );
            }}
          />

          {/* Legend */}
          <div className="flex items-center gap-5 mt-4 text-xs text-gray-500 px-1">
            <div className="flex items-center gap-1.5">
              <div className="w-4 h-3 rounded border border-green-300 bg-green-50" />
              {t("legendAutoAssigned")}
            </div>
            <div className="flex items-center gap-1.5">
              <div className="w-4 h-3 rounded border border-amber-300 bg-amber-50" />
              {t("legendSuggestion")}
            </div>
            <div className="flex items-center gap-1.5">
              <span className="text-green-600">★</span>
              {t("legendPreferred")}
            </div>
          </div>
        </div>

        {/* Right sidebar */}
        <aside className="w-80 bg-white border-l border-gray-200 flex flex-col shrink-0 overflow-auto">
          {/* Unassigned */}
          <div className="p-4 border-b border-gray-100">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-gray-900">
                {t("unassigned")}
              </h3>
              {totalUnassigned > 0 && (
                <span className="text-xs bg-red-100 text-red-700 font-medium px-2 py-0.5 rounded-full">
                  {totalUnassigned}
                </span>
              )}
            </div>

            {unassigned.length === 0 ? (
              <p className="text-xs text-gray-400">
                Iedereen is toegewezen
              </p>
            ) : (
              <div className="space-y-2">
                {unassigned.map((enrollment) => {
                  const hasPreferred = Object.values(
                    enrollment.preferences
                  ).some((p) => p === "Preferred" || p === "Available");

                  return (
                    <div
                      key={enrollment.id}
                      className={`border rounded-lg p-3 ${
                        hasPreferred
                          ? "border-amber-200 bg-amber-50/50"
                          : "border-red-200 bg-red-50/50"
                      }`}
                    >
                      <div className="flex items-center gap-2 mb-2">
                        <div
                          className={`w-7 h-7 rounded-full flex items-center justify-center text-[10px] font-bold ${
                            hasPreferred
                              ? "bg-amber-100 text-amber-700"
                              : "bg-red-100 text-red-700"
                          }`}
                        >
                          {getInitials(enrollment.studentName)}
                        </div>
                        <div>
                          <div className="text-xs font-medium text-gray-900">
                            {enrollment.studentName}
                          </div>
                          <div
                            className={`text-[10px] ${
                              hasPreferred
                                ? "text-amber-600"
                                : "text-red-600"
                            }`}
                          >
                            {hasPreferred
                              ? t("multipleOptions")
                              : t("noFittingSlot")}
                          </div>
                        </div>
                      </div>

                      {/* Preference badges */}
                      {Object.keys(enrollment.preferences).length > 0 && (
                        <div className="flex flex-wrap gap-1">
                          {Object.entries(enrollment.preferences).map(
                            ([slotId, pref]) => {
                              const slot = planning.timeSlots.find(
                                (s) => s.id === slotId
                              );
                              if (!slot) return null;
                              const isAvailable =
                                pref === "Available" || pref === "Preferred";
                              return (
                                <span
                                  key={slotId}
                                  className={`text-[10px] px-1.5 py-0.5 rounded ${
                                    pref === "Preferred"
                                      ? "bg-green-100 text-green-700"
                                      : pref === "Available"
                                        ? "bg-blue-100 text-blue-700"
                                        : "bg-gray-100 text-gray-400"
                                  }`}
                                >
                                  {DAY_NAMES_SHORT[slot.dayOfWeek]}{" "}
                                  {slot.startTime.replace(":00", "")}{" "}
                                  {isAvailable
                                    ? pref === "Preferred"
                                      ? "★"
                                      : "✓"
                                    : "✕"}
                                </span>
                              );
                            }
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Groups */}
          <div className="p-4 border-b border-gray-100">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-gray-900">
                {t("groups")}
              </h3>
            </div>

            {planning.groups.length === 0 ? (
              <p className="text-xs text-gray-400">Geen groepen</p>
            ) : (
              <div className="space-y-2">
                {planning.groups.map((group) => {
                  const slotLabel = getGroupSlotLabel(group.id);
                  const memberNames = group.memberEnrollmentIds
                    .map((id) => enrollmentMap.get(id)?.studentName ?? "?")
                    .join(", ");
                  const leaderName =
                    enrollmentMap.get(group.leaderEnrollmentId)?.studentName;

                  return (
                    <div
                      key={group.id}
                      className="border border-gray-200 rounded-lg p-3"
                    >
                      <div className="flex items-center justify-between mb-1.5">
                        <span className="text-[10px] font-bold text-green-700 bg-green-100 px-2 py-0.5 rounded">
                          {group.name}
                        </span>
                        <span className="text-[10px] text-gray-400">
                          {leaderName ? t("preFormed") : t("autoGrouped")}
                        </span>
                      </div>
                      <div className="text-[10px] text-gray-600">
                        {memberNames}
                      </div>
                      {slotLabel && (
                        <div className="text-[10px] text-gray-400 mt-1.5 flex items-center gap-1">
                          <Check size={12} className="text-green-500" />
                          {slotLabel}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Capacity per slot */}
          <div className="p-4">
            <h3 className="text-sm font-semibold text-gray-900 mb-3">
              {t("capacityPerSlot")}
            </h3>
            <div className="space-y-2">
              {planning.timeSlots
                .sort(
                  (a, b) =>
                    a.dayOfWeek - b.dayOfWeek ||
                    a.startTime.localeCompare(b.startTime)
                )
                .map((slot) => {
                  const currentCount = getSlotCurrentCount(slot.id);
                  const pct = Math.round(
                    (currentCount / slot.maxCapacity) * 100
                  );
                  const barColor =
                    currentCount >= slot.maxCapacity
                      ? "bg-red-500"
                      : currentCount >= slot.maxCapacity * 0.75
                        ? "bg-amber-500"
                        : "bg-green-500";
                  return (
                    <div key={slot.id}>
                      <div className="flex items-center justify-between text-[10px] text-gray-600 mb-1">
                        <span>
                          {DAY_NAMES_SHORT[slot.dayOfWeek]} {slot.startTime}
                          {slot.courtName && ` · ${slot.courtName}`}
                        </span>
                        <span>
                          {currentCount}/{slot.maxCapacity}
                        </span>
                      </div>
                      <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
                        <div
                          className={`h-full ${barColor} rounded-full transition-all`}
                          style={{ width: `${Math.min(pct, 100)}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
