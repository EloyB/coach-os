"use client";

import { use, useState, useMemo, useEffect, useRef } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  ArrowLeft,
  RefreshCw,
  Check,
  Users,
  User,
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
  createAssignment,
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

  // Manual assign
  const [assigningEnrollmentId, setAssigningEnrollmentId] = useState<string | null>(null);

  // Slot hover popover
  const [hoveredSlotId, setHoveredSlotId] = useState<string | null>(null);
  const hoverTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const assignMutation = useMutation({
    mutationFn: ({ enrollmentId, slotId }: { enrollmentId: string; slotId: string }) =>
      createAssignment(id, { enrollmentId, weeklyTemplateEntryId: slotId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
      setAssigningEnrollmentId(null);
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

      {/* Legend bar */}
      <div className="bg-white border-b border-gray-200 px-8 py-3 flex items-center gap-5 text-xs text-gray-500 shrink-0">
        <div className="flex items-center gap-1.5">
          <div className="w-4 h-3 rounded border border-green-300 bg-green-50" />
          {t("legendAutoAssigned")}
        </div>
        <div className="flex items-center gap-1.5">
          <div className="w-4 h-3 rounded border border-amber-300 bg-amber-50" />
          {t("legendSuggestion")}
        </div>
        <div className="flex items-center gap-1.5">
          <div className="w-4 h-3 rounded border border-blue-300 bg-blue-50" />
          {t("legendAutoGrouped")}
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
                    const slotAssignments = assignmentsBySlot.get(slot.id) ?? [];
                    const currentCount = getSlotCurrentCount(slot.id);
                    const hasProposed = slotHasProposed(slot.id);
                    const hasAutoMerged = slotAssignments.some((a) => a.isAutoMerged);
                    const borderColor = hasAutoMerged
                      ? "border-blue-300"
                      : hasProposed
                        ? "border-amber-300"
                        : "border-green-300";
                    const bgColor = hasAutoMerged
                      ? "bg-blue-50"
                      : hasProposed
                        ? "bg-amber-50"
                        : "bg-green-50";

                    return (
                      <div
                        key={slot.id}
                        className={`absolute ${bgColor} border ${borderColor} rounded-lg px-2 py-1.5 cursor-pointer shadow-sm transition-shadow z-10 ${
                          hoveredSlotId === slot.id ? "shadow-md z-30" : ""
                        }`}
                        style={{
                          top: pos.top,
                          height: pos.height,
                          left: `calc(${col.colIndex * colWidthPct}% + 1px)`,
                          width: `calc(${colWidthPct}% - 2px)`,
                        }}
                        onMouseEnter={() => {
                          if (hoverTimeoutRef.current) clearTimeout(hoverTimeoutRef.current);
                          if (slotAssignments.length > 0) setHoveredSlotId(slot.id);
                        }}
                        onMouseLeave={() => {
                          hoverTimeoutRef.current = setTimeout(() => setHoveredSlotId(null), 200);
                        }}
                      >
                        {/* Hover popover */}
                        {hoveredSlotId === slot.id && (
                          <div
                            className="absolute left-full top-0 ml-2 z-50 bg-white border border-gray-200 rounded-xl shadow-lg p-3 w-60"
                            onMouseEnter={() => {
                              if (hoverTimeoutRef.current) clearTimeout(hoverTimeoutRef.current);
                            }}
                            onMouseLeave={() => {
                              hoverTimeoutRef.current = setTimeout(() => setHoveredSlotId(null), 200);
                            }}
                          >
                            <div className="text-[11px] font-semibold text-gray-800 mb-0.5">
                              {DAY_NAMES_SHORT[slot.dayOfWeek]} {slot.startTime}–{slot.endTime}
                            </div>
                            {slot.courtName && (
                              <div className="text-[10px] text-gray-400 mb-2">{slot.courtName}</div>
                            )}
                            <div className="space-y-2.5">
                              {slotAssignments.map((assignment) => {
                                const aNames: string[] = [];
                                let gName: string | null = null;

                                if (assignment.groupId) {
                                  const group = groupMap.get(assignment.groupId);
                                  if (group) {
                                    gName = group.name;
                                    for (const mId of group.memberEnrollmentIds) {
                                      const e = enrollmentMap.get(mId);
                                      if (e) aNames.push(e.studentName);
                                    }
                                  }
                                } else if (assignment.enrollmentId) {
                                  const e = enrollmentMap.get(assignment.enrollmentId);
                                  if (e) aNames.push(e.studentName);
                                }

                                if (aNames.length === 0) return null;

                                return (
                                  <div key={assignment.id}>
                                    <div className="flex items-center gap-1.5 mb-1">
                                      {gName ? (
                                        <>
                                          <Users size={10} className="text-gray-400 shrink-0" />
                                          <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded ${
                                            assignment.isAutoMerged ? "bg-blue-100 text-blue-700" : "bg-green-100 text-green-700"
                                          }`}>
                                            {gName}
                                          </span>
                                          {assignment.isAutoMerged && <span className="text-[9px] text-blue-500 italic">auto</span>}
                                        </>
                                      ) : (
                                        <>
                                          <User size={10} className="text-gray-400 shrink-0" />
                                          <span className="text-[10px] text-gray-500">Individueel</span>
                                          {assignment.isAutoMerged && <span className="text-[9px] text-blue-500 italic">auto</span>}
                                        </>
                                      )}
                                    </div>
                                    <div className="space-y-1 pl-4">
                                      {aNames.map((name, ni) => {
                                        const aColor = getAvatarColor(name);
                                        return (
                                          <div key={ni} className="flex items-center gap-1.5">
                                            <div className={`w-4 h-4 rounded-full ${aColor.bg} ${aColor.text} flex items-center justify-center text-[7px] font-bold shrink-0`}>
                                              {getInitials(name)}
                                            </div>
                                            <span className="text-[11px] text-gray-700">{name}</span>
                                          </div>
                                        );
                                      })}
                                    </div>
                                  </div>
                                );
                              })}
                            </div>
                            <div className="mt-2 pt-2 border-t border-gray-100 text-[10px] text-gray-400">
                              {currentCount}/{slot.maxCapacity} bezet
                            </div>
                          </div>
                        )}

                        {/* Header: court + capacity + auto badge */}
                        <div className="flex items-center justify-between gap-1">
                          <div className="flex items-center gap-1 min-w-0">
                            <span className="text-[10px] font-medium text-gray-500 truncate">
                              {slot.courtName ?? ""}
                            </span>
                            {hasAutoMerged && (
                              <span className="text-[9px] text-blue-500 italic shrink-0">
                                auto
                              </span>
                            )}
                          </div>
                          <span
                            className={`text-[10px] shrink-0 ${
                              hasAutoMerged
                                ? "text-blue-600"
                                : hasProposed
                                  ? "text-amber-600"
                                  : "text-green-600"
                            }`}
                          >
                            {currentCount}/{slot.maxCapacity}
                          </span>
                        </div>

                        {/* Assigned people — flat list of avatars */}
                        {pos.height >= 36 &&
                          (() => {
                            const allNames = getSlotNames(slot.id);
                            if (allNames.length === 0) return null;

                            if (allNames.length === 1) {
                              const name = allNames[0];
                              const color = getAvatarColor(name);
                              return (
                                <div className="mt-1 flex items-center gap-1">
                                  <div
                                    title={name}
                                    className={`w-5 h-5 rounded-full ${color.bg} ${color.text} flex items-center justify-center text-[8px] font-bold shrink-0`}
                                  >
                                    {getInitials(name)}
                                  </div>
                                  <span className="text-[10px] text-gray-700 truncate">
                                    {name}
                                  </span>
                                </div>
                              );
                            }

                            return (
                              <div className="mt-1 flex items-center gap-0.5 flex-wrap">
                                {allNames.map((name, i) => {
                                  const color = getAvatarColor(name);
                                  return (
                                    <div
                                      key={i}
                                      title={name}
                                      className={`w-5 h-5 rounded-full ${color.bg} ${color.text} flex items-center justify-center text-[8px] font-bold shrink-0 cursor-default`}
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

                      {/* Manual assign: slot picker */}
                      {assigningEnrollmentId === enrollment.id ? (
                        <div className="mt-2 space-y-1.5">
                          <p className="text-[10px] text-gray-500 font-medium">
                            Kies een tijdslot:
                          </p>
                          <div className="space-y-1">
                            {planning.timeSlots
                              .sort((a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime))
                              .map((slot) => {
                                const count = getSlotCurrentCount(slot.id);
                                const isFull = count >= slot.maxCapacity;
                                return (
                                  <button
                                    key={slot.id}
                                    type="button"
                                    disabled={isFull || assignMutation.isPending}
                                    onClick={() =>
                                      assignMutation.mutate({
                                        enrollmentId: enrollment.id,
                                        slotId: slot.id,
                                      })
                                    }
                                    className={`w-full text-left px-2 py-1.5 rounded text-[10px] transition-colors ${
                                      isFull
                                        ? "bg-gray-50 text-gray-300 cursor-not-allowed"
                                        : "bg-white border border-gray-200 text-gray-700 hover:border-tennis-green hover:bg-tennis-green/5 cursor-pointer"
                                    }`}
                                  >
                                    <span className="font-medium">
                                      {DAY_NAMES_SHORT[slot.dayOfWeek]}{" "}
                                      {slot.startTime}–{slot.endTime}
                                    </span>
                                    {slot.courtName && (
                                      <span className="text-gray-400 ml-1">
                                        · {slot.courtName}
                                      </span>
                                    )}
                                    <span className="float-right text-gray-400">
                                      {count}/{slot.maxCapacity}
                                    </span>
                                  </button>
                                );
                              })}
                          </div>
                          <button
                            type="button"
                            onClick={() => setAssigningEnrollmentId(null)}
                            className="text-[10px] text-gray-400 hover:text-gray-600"
                          >
                            Annuleren
                          </button>
                        </div>
                      ) : (
                        <button
                          type="button"
                          onClick={() =>
                            setAssigningEnrollmentId(enrollment.id)
                          }
                          className="mt-2 text-[10px] text-tennis-green hover:underline font-medium"
                        >
                          {t("assignManually")} →
                        </button>
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
                  const groupAssignment = planning.assignments.find(
                    (a) => a.groupId === group.id
                  );
                  const isAutoMerged = groupAssignment?.isAutoMerged ?? false;

                  return (
                    <div
                      key={group.id}
                      className={`border rounded-lg p-3 ${
                        isAutoMerged
                          ? "border-blue-200 bg-blue-50/30"
                          : "border-gray-200"
                      }`}
                    >
                      <div className="flex items-center justify-between mb-1.5">
                        <span className="text-[10px] font-bold text-green-700 bg-green-100 px-2 py-0.5 rounded">
                          {group.name}
                        </span>
                        <span className={`text-[10px] ${isAutoMerged ? "text-blue-500 italic" : "text-gray-400"}`}>
                          {isAutoMerged ? t("autoGrouped") : t("preFormed")}
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
