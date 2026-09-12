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
  Mail,
  Lock,
  Unlock,
  Plus,
  ChevronDown,
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
  deleteAssignment,
  lockAssignment,
  unlockAssignment,
  sendAssignmentConfirmation,
} from "@/lib/api/planning";
import type {
  PlanningEnrollmentDto,
  PlanningAssignmentDto,
  PlanningGroupDto,
} from "@/lib/api/planning";
import { getLessonSeriesById, deleteWeekSlot } from "@/lib/api/lessonSeries";
import { getTrainers } from "@/lib/api/trainers";
import { getLessonSeriesEnrollments } from "@/lib/api/enrollments";
import type { LessonSeriesEnrollmentDto } from "@/lib/api/enrollments";
import { EnrollmentDetailDialog } from "../_components/enrollment-detail-dialog";
import {
  HoverCard,
  HoverCardTrigger,
  HoverCardContent,
} from "@/components/ui/hover-card";
import {
  AddWeekSlotDialog,
  type WeekSlotEditData,
} from "../_components/add-week-slot-dialog";
import { NonRespondersPanel } from "@/components/dashboard/non-responders-panel";
import {
  CalendarGrid,
  parseTime,
  getSlotPosition,
  layoutDaySlots,
  type CalendarSlot,
} from "@/components/calendar/calendar-grid";
import { getInitials, getAvatarColor } from "@/lib/planning-avatars";
import { TimeslotDetailDialog } from "./_components/timeslot-detail-dialog";
import { isHeadTrainerViewer } from "@/lib/auth";

// ─── Constants ───────────────────────────────────────────────────────────────

const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

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
  // Hoofdtrainer = read-only: enkel de planning raadplegen, geen bewerkacties.
  // Reactief via effect zodat het na hydration klopt (localStorage is er niet bij SSR).
  const [readOnly, setReadOnly] = useState(false);
  useEffect(() => setReadOnly(isHeadTrainerViewer()), []);

  const { data: series } = useQuery({
    queryKey: ["lessonSeries", id],
    queryFn: () => getLessonSeriesById(id),
  });

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const {
    data: planning,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["planning", id],
    queryFn: () => getPlanningOverview(id),
  });

  // Volledige inschrijvingen (DOB, prijsoptie, formuliervragen, …) om de
  // detail-dialog vanaf de planning te kunnen tonen. Zelfde query als de
  // reeks-tabel, dus meestal al gecached.
  const { data: fullEnrollments = [] } = useQuery({
    queryKey: ["enrollments", id],
    queryFn: () => getLessonSeriesEnrollments(id),
  });

  // Klik op een persoon/groep → detail-dialog in kijkmodus (geen bewerk-callbacks).
  const [detailTarget, setDetailTarget] = useState<{
    enrollment: LessonSeriesEnrollmentDto;
    groupMembers?: LessonSeriesEnrollmentDto[];
  } | null>(null);


  const generateMutation = useMutation({
    mutationFn: (force: boolean = false) => generatePlanning(id, force),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
    },
  });

  const unassignMutation = useMutation({
    mutationFn: (assignmentId: string) => deleteAssignment(id, assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
    },
  });

  const lockMutation = useMutation({
    mutationFn: ({ assignmentId, isLocked }: { assignmentId: string; isLocked: boolean }) =>
      isLocked ? unlockAssignment(id, assignmentId) : lockAssignment(id, assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
    },
  });

  const sendConfirmationMutation = useMutation({
    mutationFn: (assignmentId: string) => sendAssignmentConfirmation(id, assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", id] });
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

  // Toewijs-modus: klik 'Toewijzen' op een sidebar-kaart → de kalendertegels
  // kleuren naar de beschikbaarheid van deze persoon/groep en klikken op een
  // tegel wijst toe. `prefs` = de voorkeuren om op te kleuren (leider bij groep).
  const [assignTarget, setAssignTarget] = useState<
    | { kind: "solo"; enrollmentId: string; name: string; size: number; prefs: Record<string, string> }
    | { kind: "group"; groupId: string; name: string; size: number; prefs: Record<string, string> }
    | null
  >(null);

  // Niet-toegewezen: uitklapbaar, default open (het is de actieve werklijst).
  const [showUnassigned, setShowUnassigned] = useState(true);
  // Toegewezen-sectie: default ingeklapt; per eenheid een extra-slot-kiezer.
  const [showAssigned, setShowAssigned] = useState(false);
  const [addingSlotForKey, setAddingSlotForKey] = useState<string | null>(null);
  // Bevestiging vóór 'Definitief aanbieden' vanuit de Toegewezen-sectie
  // (verstuurt meteen e-mail-aanbod(en) voor alle voorstellen van de eenheid).
  const [offerTarget, setOfferTarget] = useState<{ ids: string[]; name: string } | null>(null);

  // Slot detail dialog (click to open)
  const [openSlotId, setOpenSlotId] = useState<string | null>(null);
  const [addingSlot, setAddingSlot] = useState(false);
  const [editingSlot, setEditingSlot] = useState<WeekSlotEditData | null>(null);

  const deleteSlotMutation = useMutation({
    mutationFn: (entryId: string) => deleteWeekSlot(id, entryId),
    onSuccess: () => {
      setOpenSlotId(null);
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", id] });
    },
  });

  const assignMutation = useMutation({
    mutationFn: ({ enrollmentId, groupId, slotId }: { enrollmentId?: string; groupId?: string; slotId: string }) =>
      createAssignment(id, { enrollmentId, groupId, weeklyTemplateEntryId: slotId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning", id] });
      setAssignTarget(null);
    },
  });

  // Esc sluit de toewijs-modus.
  useEffect(() => {
    if (!assignTarget) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setAssignTarget(null);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [assignTarget]);

  // Auto-generate only once, only when status is "Enrollment" (never generated before)
  const hasAutoGeneratedRef = useRef(false);
  useEffect(() => {
    if (isHeadTrainerViewer()) return;
    if (!hasAutoGeneratedRef.current && planning && planning.planningStatus === "Enrollment") {
      hasAutoGeneratedRef.current = true;
      generateMutation.mutate(false);
    }
  }, [planning, generateMutation]);

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

  const fullEnrollmentMap = useMemo(() => {
    const map = new Map<string, LessonSeriesEnrollmentDto>();
    for (const e of fullEnrollments) map.set(e.id, e);
    return map;
  }, [fullEnrollments]);

  // Assignments by timeSlotId
  const assignmentsBySlot = useMemo(() => {
    const map = new Map<string, PlanningAssignmentDto[]>();
    if (!planning) return map;
    for (const a of planning.assignments) {
      if (a.status === "Declined") continue;
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

  // Groups that have an assignment
  const assignedGroupIds = useMemo(() => {
    const set = new Set<string>();
    if (!planning) return set;
    for (const a of planning.assignments) {
      if (a.groupId) set.add(a.groupId);
    }
    return set;
  }, [planning]);

  // Unassigned: split into solo enrollees and unassigned groups
  const { unassignedSolos, unassignedGroups } = useMemo(() => {
    if (!planning) return { unassignedSolos: [], unassignedGroups: [] };

    const solos: PlanningEnrollmentDto[] = [];
    const groupIds = new Set<string>();

    for (const e of planning.enrollments) {
      if (assignedEnrollmentIds.has(e.id)) continue;

      if (e.groupId) {
        // Only add the group once, and only if the group itself has no assignment
        if (!assignedGroupIds.has(e.groupId) && !groupIds.has(e.groupId)) {
          groupIds.add(e.groupId);
        }
      } else {
        solos.push(e);
      }
    }

    const groups = planning.groups.filter((g) => groupIds.has(g.id));
    return { unassignedSolos: solos, unassignedGroups: groups };
  }, [planning, assignedEnrollmentIds, assignedGroupIds]);

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

  // Toegewezen eenheden (solo of groep), met hun slot(s) — één rij per persoon/groep.
  const assignedUnits = useMemo(() => {
    if (!planning) return [];
    type Unit = {
      key: string;
      type: "solo" | "group";
      name: string;
      target: { enrollmentId?: string; groupId?: string };
      rep: PlanningAssignmentDto;
      assignments: PlanningAssignmentDto[];
      slots: { id: string; label: string }[];
    };
    const byKey = new Map<string, Unit>();
    for (const a of planning.assignments) {
      if (a.status === "Declined") continue;
      let key: string, type: "solo" | "group", name: string;
      let target: { enrollmentId?: string; groupId?: string };
      if (a.groupId) {
        const g = groupMap.get(a.groupId);
        if (!g) continue;
        key = `g:${a.groupId}`;
        type = "group";
        name = g.name;
        target = { groupId: a.groupId };
      } else if (a.enrollmentId) {
        const e = enrollmentMap.get(a.enrollmentId);
        if (!e) continue;
        key = `e:${a.enrollmentId}`;
        type = "solo";
        name = e.studentName;
        target = { enrollmentId: a.enrollmentId };
      } else {
        continue;
      }
      const slot = planning.timeSlots.find((s) => s.id === a.timeSlotId);
      const label = slot ? `${DAY_NAMES_SHORT[slot.dayOfWeek]} ${slot.startTime}` : "?";
      const existing = byKey.get(key);
      if (existing) {
        existing.slots.push({ id: a.timeSlotId, label });
        existing.assignments.push(a);
      } else {
        byKey.set(key, {
          key, type, name, target, rep: a,
          assignments: [a],
          slots: [{ id: a.timeSlotId, label }],
        });
      }
    }
    return [...byKey.values()].sort((a, b) => a.name.localeCompare(b.name));
  }, [planning, groupMap, enrollmentMap]);

  // Stats
  const totalUnassigned = unassignedSolos.length + unassignedGroups.length;
  const totalSlots = planning?.timeSlots.length ?? 0;
  const totalCapacity =
    planning?.timeSlots.reduce((sum, s) => sum + s.maxCapacity, 0) ?? 0;
  const totalEnrollments = planning?.enrollments.length ?? 0;
  const lockedAssignmentsCount =
    planning?.assignments.filter((assignment) => assignment.isLocked).length ?? 0;

  // Helper: get names for a slot's assignments
  function getSlotNames(slotId: string): string[] {
    return getSlotPeople(slotId).map((p) => p.name);
  }

  // Zelfde als getSlotNames, maar met de enrollment-id per persoon zodat de
  // avatar/naam klikbaar is naar de detail-dialog.
  function getSlotPeople(slotId: string): { name: string; enrollmentId: string }[] {
    const assignments = assignmentsBySlot.get(slotId) ?? [];
    const people: { name: string; enrollmentId: string }[] = [];
    for (const a of assignments) {
      if (a.enrollmentId) {
        const e = enrollmentMap.get(a.enrollmentId);
        if (e) people.push({ name: e.studentName, enrollmentId: e.id });
      } else if (a.groupId) {
        const g = groupMap.get(a.groupId);
        if (g) {
          for (const memberId of g.memberEnrollmentIds) {
            const e = enrollmentMap.get(memberId);
            if (e) people.push({ name: e.studentName, enrollmentId: e.id });
          }
        }
      }
    }
    return people;
  }

  function getSlotCurrentCount(slotId: string): number {
    return getSlotPeople(slotId).length;
  }

  // Detail-dialog openers (kijkmodus). Sluit de slot-dialog zodat er geen
  // modals stapelen.
  function openPersonDetail(enrollmentId: string) {
    const e = fullEnrollmentMap.get(enrollmentId);
    if (!e) return;
    setOpenSlotId(null);
    setDetailTarget({ enrollment: e });
  }

  function openGroupDetail(groupId: string) {
    const group = groupMap.get(groupId);
    if (!group) return;
    const leader = fullEnrollmentMap.get(group.leaderEnrollmentId);
    if (!leader) return;
    const members = group.memberEnrollmentIds
      .map((mid) => fullEnrollmentMap.get(mid))
      .filter((e): e is LessonSeriesEnrollmentDto => e != null);
    setOpenSlotId(null);
    setDetailTarget({ enrollment: leader, groupMembers: members });
  }

  // Multi-slot: slots waar deze persoon/groep nog extra bij kan (niet het huidige
  // of een reeds toegewezen slot, en met genoeg vrije plaats).
  function eligibleExtraSlots(assignment: PlanningAssignmentDto) {
    if (!planning) return [];
    const size = assignment.groupId
      ? groupMap.get(assignment.groupId)?.memberEnrollmentIds.length ?? 1
      : 1;
    const takenSlotIds = new Set(
      planning.assignments
        .filter((a) =>
          assignment.groupId
            ? a.groupId === assignment.groupId
            : a.enrollmentId != null && a.enrollmentId === assignment.enrollmentId
        )
        .map((a) => a.timeSlotId)
    );
    return planning.timeSlots
      .filter((s) => !takenSlotIds.has(s.id))
      .map((s) => ({
        id: s.id,
        dayOfWeek: s.dayOfWeek,
        startTime: s.startTime,
        endTime: s.endTime,
        courtName: s.courtName,
        remaining: s.maxCapacity - getSlotCurrentCount(s.id),
      }))
      .filter((s) => s.remaining >= size)
      .sort(
        (a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime)
      );
  }

  function slotHasProposed(slotId: string): boolean {
    const assignments = assignmentsBySlot.get(slotId) ?? [];
    return assignments.some((a) => a.status === "Proposed");
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
    // Breekt uit de layout-padding (main = px-7 py-6 / lg:pb-6) en vult de volle
    // hoogte: h = 100% van de content-box + de 3rem verticale padding, zodat de
    // agenda + zijkolom tot onderaan lopen (geen lege balk).
    <div className="flex flex-col h-[calc(100%_+_3rem)] -mx-7 -my-6">
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
              {t("concept")}
            </span>
          )}
          {planning.planningStatus === "AwaitingConfirmation" && (
            <span className="px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
              Wacht op bevestiging
            </span>
          )}
          {planning.planningStatus === "Scheduled" && (
            <span className="px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
              Bevestigd
            </span>
          )}
          {planning.planningLastEditedAt && (
            <span className="text-xs text-gray-400">
              {t("lastEdited")}: {new Date(planning.planningLastEditedAt).toLocaleDateString("nl-BE", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" })}
            </span>
          )}
        </div>
        {!readOnly && planning.planningStatus !== "Scheduled" && (
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setAddingSlot(true)}
              className="inline-flex items-center gap-2 border border-gray-300 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition"
            >
              <Plus size={15} />
              {t("addSlot")}
            </button>
            <Link
              href={`/dashboard/lessons/${id}`}
              className="inline-flex items-center gap-2 border border-gray-300 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition"
            >
              {t("goBack")}
            </Link>

            <AlertDialog>
              <AlertDialogTrigger asChild>
                <button
                  type="button"
                  disabled={generateMutation.isPending}
                  className="inline-flex items-center gap-2 border border-gray-300 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition disabled:opacity-50"
                >
                  <RefreshCw
                    size={16}
                    className={generateMutation.isPending ? "animate-spin" : ""}
                  />
                  {generateMutation.isPending ? t("generating") : t("regenerate")}
                </button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>{t("regenerateConfirmTitle")}</AlertDialogTitle>
                  <AlertDialogDescription>
                    {t("regenerateKeepOrOverwrite")}
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter className="flex-col sm:flex-row gap-2">
                  <AlertDialogCancel>Annuleren</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={() => generateMutation.mutate(false)}
                    className="bg-tennis-green hover:bg-tennis-green/90"
                  >
                    <Lock size={14} className="mr-1.5" />
                    {t("regenerateKeep")}
                  </AlertDialogAction>
                  <AlertDialogAction
                    onClick={() => generateMutation.mutate(true)}
                    className="bg-amber-600 hover:bg-amber-700"
                  >
                    <RefreshCw size={14} className="mr-1.5" />
                    {t("regenerateOverwrite")}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>

            <AlertDialog>
              <AlertDialogTrigger asChild>
                <button
                  type="button"
                  disabled={confirmMutation.isPending || totalUnassigned > 0}
                  title={
                    totalUnassigned > 0
                      ? t("confirmDisabledUnassigned", { count: totalUnassigned })
                      : undefined
                  }
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
        )}
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
          {t("enrollmentsCount", { count: totalEnrollments })} ·{" "}
          {t("timeSlotsCount", { count: totalSlots })} ·{" "}
          {t("spotsCount", { count: totalCapacity })}
        </div>
      </div>

      {/* Toewijs-modus banner */}
      {assignTarget && (
        <div className="bg-tennis-green/10 border-b border-tennis-green/20 px-8 py-3 shrink-0">
          <div className="flex items-center gap-3 text-sm text-tennis-green">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-tennis-green/15 text-tennis-green">
              <Check size={16} />
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-semibold">
                Kies een tijdslot voor {assignTarget.name}
              </p>
              <p className="text-xs text-tennis-green/70">
                <span className="inline-block h-2 w-2 rounded-full bg-green-500 align-middle" /> voorkeur ·{" "}
                <span className="inline-block h-2 w-2 rounded-full bg-blue-500 align-middle" /> beschikbaar ·{" "}
                <span className="inline-block h-2 w-2 rounded-full bg-gray-300 align-middle" /> niet beschikbaar/vol · Esc om te annuleren
              </p>
            </div>
            <button
              type="button"
              onClick={() => setAssignTarget(null)}
              className="rounded-lg border border-tennis-green/30 bg-white px-3 py-1.5 text-xs font-medium text-tennis-green hover:bg-tennis-green/5"
            >
              Annuleren
            </button>
          </div>
        </div>
      )}

      {!readOnly && planning.planningStatus !== "Scheduled" && !assignTarget && (
        <div className="bg-amber-50 border-b border-amber-100 px-8 py-3 shrink-0">
          <div className="flex items-center gap-3 text-sm text-amber-900">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-amber-100 text-amber-700">
              <Lock size={16} />
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-semibold">{t("lockHelpTitle")}</p>
              <p className="text-xs text-amber-700">{t("lockHelpDesc")}</p>
            </div>
            {lockedAssignmentsCount > 0 && (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-white px-2.5 py-1 text-xs font-semibold text-tennis-green shadow-sm">
                <Lock size={12} />
                {t("lockedCount", { count: lockedAssignmentsCount })}
              </span>
            )}
          </div>
        </div>
      )}

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
                    const lockedAssignment = slotAssignments.find((a) => a.isLocked);

                    // Toewijs-modus: kleur naar de voorkeur van de geselecteerde
                    // persoon/groep en bepaal of ze er nog bij passen.
                    const assignPref = assignTarget?.prefs[slot.id];
                    const assignFits =
                      assignTarget != null &&
                      slot.maxCapacity - currentCount >= assignTarget.size;

                    const borderColor = assignTarget
                      ? !assignFits
                        ? "border-gray-200"
                        : assignPref === "Preferred"
                          ? "border-green-500"
                          : assignPref === "Available"
                            ? "border-blue-500"
                            : "border-gray-300"
                      : lockedAssignment
                        ? "border-tennis-green"
                        : hasAutoMerged
                          ? "border-blue-300"
                          : hasProposed
                            ? "border-amber-300"
                            : "border-green-300";
                    const bgColor = assignTarget
                      ? !assignFits
                        ? "bg-gray-50"
                        : assignPref === "Preferred"
                          ? "bg-green-100"
                          : assignPref === "Available"
                            ? "bg-blue-100"
                            : "bg-gray-100/60"
                      : lockedAssignment
                        ? "bg-green-50"
                        : hasAutoMerged
                          ? "bg-blue-50"
                          : hasProposed
                            ? "bg-amber-50"
                            : "bg-green-50";

                    // In toewijs-modus is een volle tegel niet klikbaar; anders
                    // wijst een klik toe (ook een 'niet beschikbaar'-tegel, als
                    // bewuste overschrijving). Buiten toewijs-modus opent de dialog.
                    const assignable = assignTarget != null && assignFits;
                    const handleTileClick = () => {
                      if (assignTarget) {
                        if (!assignFits) return;
                        assignMutation.mutate(
                          assignTarget.kind === "solo"
                            ? { enrollmentId: assignTarget.enrollmentId, slotId: slot.id }
                            : { groupId: assignTarget.groupId, slotId: slot.id }
                        );
                        return;
                      }
                      setOpenSlotId(slot.id);
                    };

                    return (
                      <HoverCard key={slot.id} openDelay={120} closeDelay={80}>
                        <HoverCardTrigger asChild>
                      <div
                        className={`absolute ${bgColor} border ${borderColor} rounded-lg px-2 py-1.5 shadow-sm transition-shadow z-10 hover:shadow-md ${
                          assignTarget
                            ? assignable
                              ? "cursor-pointer ring-2 ring-offset-1 ring-tennis-green/30"
                              : "cursor-not-allowed opacity-60"
                            : "cursor-pointer"
                        }`}
                        style={{
                          top: pos.top,
                          height: pos.height,
                          left: `calc(${col.colIndex * colWidthPct}% + 1px)`,
                          width: `calc(${colWidthPct}% - 2px)`,
                        }}
                        onClick={handleTileClick}
                      >
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
                          <div className="flex items-center gap-1 shrink-0">
                            <span
                              className={`text-[10px] ${
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
                        </div>

                        {/* Assigned people — flat list of avatars (klikbaar → detail) */}
                        {pos.height >= 36 &&
                          (() => {
                            const people = getSlotPeople(slot.id);
                            if (people.length === 0) return null;

                            if (people.length === 1) {
                              const { name, enrollmentId } = people[0];
                              const color = getAvatarColor(name);
                              return (
                                <button
                                  type="button"
                                  title={name}
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    openPersonDetail(enrollmentId);
                                  }}
                                  className="mt-1 flex cursor-pointer items-center gap-1 rounded px-0.5 hover:bg-white/70"
                                >
                                  <div
                                    className={`w-5 h-5 rounded-full ${color.bg} ${color.text} flex items-center justify-center text-[8px] font-bold shrink-0`}
                                  >
                                    {getInitials(name)}
                                  </div>
                                  <span className="text-[10px] text-gray-700 truncate">
                                    {name}
                                  </span>
                                </button>
                              );
                            }

                            return (
                              <div className="mt-1 flex items-center gap-0.5 flex-wrap">
                                {people.map((person, i) => {
                                  const color = getAvatarColor(person.name);
                                  return (
                                    <button
                                      key={i}
                                      type="button"
                                      title={person.name}
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        openPersonDetail(person.enrollmentId);
                                      }}
                                      className={`w-5 h-5 rounded-full ${color.bg} ${color.text} flex items-center justify-center text-[8px] font-bold shrink-0 cursor-pointer hover:ring-2 hover:ring-white`}
                                    >
                                      {getInitials(person.name)}
                                    </button>
                                  );
                                })}
                              </div>
                            );
                          })()}
                      </div>
                        </HoverCardTrigger>
                        {slotAssignments.length > 0 && (
                          <HoverCardContent
                            side="right"
                            align="start"
                            sideOffset={8}
                            collisionPadding={12}
                            className="w-64 p-3"
                          >
                            <div className="text-[11px] font-semibold text-gray-800 mb-0.5">
                              {DAY_NAMES_SHORT[slot.dayOfWeek]} {slot.startTime}–{slot.endTime}
                            </div>
                            <div className="text-[10px] text-gray-400 mb-2">
                              {[slot.courtName, slot.trainerName].filter(Boolean).join(" · ")}
                            </div>
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
                                    {(gName || assignment.isLocked) && (
                                      <div className="flex items-center gap-1.5 mb-1">
                                        {gName && (
                                          <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded ${
                                            assignment.isAutoMerged ? "bg-blue-100 text-blue-700" : "bg-green-100 text-green-700"
                                          }`}>
                                            {gName}
                                          </span>
                                        )}
                                        {assignment.isAutoMerged && <span className="text-[9px] text-blue-500 italic">auto</span>}
                                        {assignment.isLocked && (
                                          <span className="inline-flex items-center gap-1 rounded bg-green-100 px-1.5 py-0.5 text-[9px] font-semibold text-green-700">
                                            <Lock size={9} />
                                            {t("locked")}
                                          </span>
                                        )}
                                      </div>
                                    )}
                                    <div className={`space-y-1 ${gName ? "pl-2" : ""}`}>
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
                              {t("occupied", { count: currentCount, max: slot.maxCapacity })}
                            </div>
                          </HoverCardContent>
                        )}
                      </HoverCard>
                    );
                  })}
                </>
              );
            }}
          />

        </div>

        {/* Right sidebar */}
        <aside className="w-80 bg-white border-l border-gray-200 flex flex-col shrink-0 overflow-auto">
          {/* Non-responders (only in AwaitingConfirmation) */}
          {planning.planningStatus === "AwaitingConfirmation" && (
            <NonRespondersPanel seriesId={id} />
          )}

          {/* Unassigned (uitklapbaar) */}
          <div className="p-4 border-b border-gray-100">
            <button
              type="button"
              onClick={() => setShowUnassigned((v) => !v)}
              className="mb-3 flex w-full cursor-pointer items-center justify-between"
            >
              <span className="flex items-center gap-2 text-sm font-semibold text-gray-900">
                {t("unassigned")}
                {totalUnassigned > 0 && (
                  <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-700">
                    {totalUnassigned}
                  </span>
                )}
              </span>
              <ChevronDown
                size={16}
                className={`text-gray-400 transition-transform ${showUnassigned ? "rotate-180" : ""}`}
              />
            </button>

            {showUnassigned &&
              (totalUnassigned === 0 ? (
              <p className="text-xs text-gray-400">
                Iedereen is toegewezen
              </p>
            ) : (
              <div className="space-y-2">
                {/* Unassigned groups */}
                {unassignedGroups.map((group) => {
                  const members = group.memberEnrollmentIds
                    .map((mId) => enrollmentMap.get(mId))
                    .filter(Boolean) as PlanningEnrollmentDto[];
                  const leader = enrollmentMap.get(group.leaderEnrollmentId);

                  return (
                    <div
                      key={group.id}
                      className="rounded-lg border border-amber-200 bg-amber-50/50 p-2.5"
                    >
                      <div className="flex items-center gap-2">
                        <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-amber-100 text-amber-700">
                          <Users size={13} />
                        </div>
                        <div className="min-w-0 flex-1">
                          <button
                            type="button"
                            onClick={() => openGroupDetail(group.id)}
                            className="block max-w-full cursor-pointer truncate text-left text-xs font-medium text-gray-900 hover:text-tennis-green hover:underline"
                          >
                            {group.name}
                          </button>
                          <div className="text-[10px] text-amber-600">{members.length} leden</div>
                        </div>
                        {!readOnly && leader && (() => {
                          const active =
                            assignTarget?.kind === "group" && assignTarget.groupId === group.id;
                          return (
                            <button
                              type="button"
                              onClick={() =>
                                setAssignTarget(
                                  active
                                    ? null
                                    : {
                                        kind: "group",
                                        groupId: group.id,
                                        name: group.name,
                                        size: members.length,
                                        prefs: leader.preferences,
                                      }
                                )
                              }
                              className={`shrink-0 cursor-pointer rounded-md border px-2 py-1 text-[10px] font-semibold transition-colors ${
                                active
                                  ? "border-gray-200 text-gray-500 hover:bg-gray-50"
                                  : "border-tennis-green/30 text-tennis-green hover:bg-tennis-green/5"
                              }`}
                            >
                              {active ? t("cancelAssign") : t("assignShort")}
                            </button>
                          );
                        })()}
                      </div>

                      <div className="mt-2 flex flex-wrap gap-1">
                        {members.map((m) => {
                          const color = getAvatarColor(m.studentName);
                          const isLeader = m.id === group.leaderEnrollmentId;
                          return (
                            <button
                              key={m.id}
                              type="button"
                              onClick={() => openPersonDetail(m.id)}
                              title={isLeader ? `${m.studentName} (leider)` : m.studentName}
                              className="flex cursor-pointer items-center gap-1 rounded-full bg-white/70 py-0.5 pl-0.5 pr-2 transition-colors hover:bg-white"
                            >
                              <div className={`flex h-4 w-4 shrink-0 items-center justify-center rounded-full text-[7px] font-bold ${color.bg} ${color.text}`}>
                                {getInitials(m.studentName)}
                              </div>
                              <span className="max-w-[80px] truncate text-[10px] text-gray-700">
                                {m.studentName}
                              </span>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  );
                })}

                {/* Unassigned solos */}
                {unassignedSolos.map((enrollment) => {
                  const availableSlotIds = Object.entries(enrollment.preferences)
                    .filter(([, p]) => p === "Preferred" || p === "Available")
                    .map(([id]) => id);
                  const hasPreferred = availableSlotIds.length > 0;
                  const allAvailableAreFull = hasPreferred && availableSlotIds.every((slotId) => {
                    const slot = planning.timeSlots.find((s) => s.id === slotId);
                    if (!slot) return true;
                    const count = (assignmentsBySlot.get(slot.id) ?? []).reduce((acc, a) => {
                      if (a.enrollmentId) return acc + 1;
                      if (a.groupId) {
                        const g = groupMap.get(a.groupId);
                        return acc + (g?.memberEnrollmentIds.length ?? 0);
                      }
                      return acc;
                    }, 0);
                    return count >= slot.maxCapacity;
                  });
                  const noSlotsConfigured = planning.timeSlots.length === 0;
                  // Toon enkel een statusregel bij een probleem; is de persoon
                  // gewoon plaatsbaar (heeft opties met plaats), dan geen ruis.
                  const reasonText = noSlotsConfigured
                    ? t("noSlotsAvailable")
                    : allAvailableAreFull
                      ? t("noSlotCapacity")
                      : hasPreferred
                        ? null
                        : t("noFittingSlot");

                  return (
                    <div
                      key={enrollment.id}
                      className={`rounded-lg border p-2.5 ${
                        hasPreferred && !allAvailableAreFull
                          ? "border-amber-200 bg-amber-50/50"
                          : "border-red-200 bg-red-50/50"
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <div
                          className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-[10px] font-bold ${
                            hasPreferred && !allAvailableAreFull
                              ? "bg-amber-100 text-amber-700"
                              : "bg-red-100 text-red-700"
                          }`}
                        >
                          {getInitials(enrollment.studentName)}
                        </div>
                        <div className="min-w-0 flex-1">
                          <button
                            type="button"
                            onClick={() => openPersonDetail(enrollment.id)}
                            className="block max-w-full cursor-pointer truncate text-left text-xs font-medium text-gray-900 hover:text-tennis-green hover:underline"
                          >
                            {enrollment.studentName}
                          </button>
                          {reasonText && (
                            <div className="text-[10px] text-red-600">
                              {reasonText}
                            </div>
                          )}
                        </div>
                        {!readOnly && (() => {
                          const active =
                            assignTarget?.kind === "solo" &&
                            assignTarget.enrollmentId === enrollment.id;
                          return (
                            <button
                              type="button"
                              onClick={() =>
                                setAssignTarget(
                                  active
                                    ? null
                                    : {
                                        kind: "solo",
                                        enrollmentId: enrollment.id,
                                        name: enrollment.studentName,
                                        size: 1,
                                        prefs: enrollment.preferences,
                                      }
                                )
                              }
                              className={`shrink-0 cursor-pointer rounded-md border px-2 py-1 text-[10px] font-semibold transition-colors ${
                                active
                                  ? "border-gray-200 text-gray-500 hover:bg-gray-50"
                                  : "border-tennis-green/30 text-tennis-green hover:bg-tennis-green/5"
                              }`}
                            >
                              {active ? t("cancelAssign") : t("assignShort")}
                            </button>
                          );
                        })()}
                      </div>
                    </div>
                  );
                })}
              </div>
            ))}
          </div>

          {/* Toegewezen (uitklapbaar, default ingeklapt) */}
          <div className="p-4 border-b border-gray-100">
            <button
              type="button"
              onClick={() => setShowAssigned((v) => !v)}
              className="flex w-full cursor-pointer items-center justify-between"
            >
              <span className="flex items-center gap-2 text-sm font-semibold text-gray-900">
                {t("assigned")}
                {assignedUnits.length > 0 && (
                  <span className="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
                    {assignedUnits.length}
                  </span>
                )}
              </span>
              <ChevronDown
                size={16}
                className={`text-gray-400 transition-transform ${showAssigned ? "rotate-180" : ""}`}
              />
            </button>

            {showAssigned && (
              <div className="mt-3 space-y-2">
                {assignedUnits.length === 0 ? (
                  <p className="text-xs text-gray-400">{t("nobodyAssigned")}</p>
                ) : (
                  assignedUnits.map((unit) => {
                    const options = eligibleExtraSlots(unit.rep);
                    const isOpen = addingSlotForKey === unit.key;
                    // Lock/aanbieden werken op alle nog-voorgestelde toewijzingen
                    // van deze eenheid (kan meerdere slots zijn bij multi-slot).
                    const proposed = unit.assignments.filter(
                      (a) => a.status === "Proposed"
                    );
                    const canOffer = proposed.length > 0;
                    const allLocked = canOffer && proposed.every((a) => a.isLocked);
                    return (
                      <div key={unit.key} className="rounded-lg border border-gray-200 p-2.5">
                        <div className="flex items-center gap-2">
                          <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gray-100 text-gray-500">
                            {unit.type === "group" ? (
                              <Users size={13} />
                            ) : (
                              <span className="text-[10px] font-bold">{getInitials(unit.name)}</span>
                            )}
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="truncate text-xs font-medium text-gray-900">
                              {unit.name}
                            </div>
                            <div className="mt-0.5 flex flex-wrap gap-1">
                              {unit.slots.map((s, i) => (
                                <span
                                  key={i}
                                  className="rounded bg-gray-100 px-1.5 py-0.5 text-[10px] text-gray-600"
                                >
                                  {s.label}
                                </span>
                              ))}
                            </div>
                          </div>
                        </div>

                        {!readOnly && (
                          <div className="mt-2 border-t border-gray-100 pt-2">
                            {isOpen ? (
                              <div className="space-y-1.5">
                                <p className="text-[10px] font-medium text-gray-500">
                                  {t("chooseExtraSlot")}
                                </p>
                                {options.length === 0 ? (
                                  <p className="text-[10px] text-gray-400">
                                    {t("noOtherSlotAvailable")}
                                  </p>
                                ) : (
                                  <div className="space-y-1">
                                    {options.map((s) => (
                                      <button
                                        key={s.id}
                                        type="button"
                                        disabled={assignMutation.isPending}
                                        onClick={() => {
                                          assignMutation.mutate({ ...unit.target, slotId: s.id });
                                          setAddingSlotForKey(null);
                                        }}
                                        className="w-full cursor-pointer rounded-md border border-gray-200 px-2 py-1.5 text-left text-[10px] text-gray-700 transition-colors hover:border-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
                                      >
                                        <span className="font-medium">
                                          {DAY_NAMES_SHORT[s.dayOfWeek]} {s.startTime}–{s.endTime}
                                        </span>
                                        {s.courtName && (
                                          <span className="ml-1 text-gray-400">· {s.courtName}</span>
                                        )}
                                      </button>
                                    ))}
                                  </div>
                                )}
                                <button
                                  type="button"
                                  onClick={() => setAddingSlotForKey(null)}
                                  className="cursor-pointer text-[10px] text-gray-400 hover:text-gray-600"
                                >
                                  {t("cancel")}
                                </button>
                              </div>
                            ) : (
                              <div className="flex items-center gap-1">
                                <button
                                  type="button"
                                  onClick={() => setAddingSlotForKey(unit.key)}
                                  className="inline-flex cursor-pointer items-center gap-1 text-[11px] font-medium text-tennis-green hover:underline"
                                >
                                  <Plus size={12} />
                                  {t("addExtraSlot")}
                                </button>
                                {canOffer && (
                                  <div className="ml-auto flex items-center gap-0.5">
                                    <button
                                      type="button"
                                      title={
                                        allLocked
                                          ? t("unlock")
                                          : unit.type === "group"
                                            ? t("lockGroup")
                                            : t("lock")
                                      }
                                      aria-label={allLocked ? t("unlock") : t("lock")}
                                      onClick={() => {
                                        if (allLocked) {
                                          proposed
                                            .filter((a) => a.isLocked)
                                            .forEach((a) =>
                                              lockMutation.mutate({ assignmentId: a.id, isLocked: true })
                                            );
                                        } else {
                                          proposed
                                            .filter((a) => !a.isLocked)
                                            .forEach((a) =>
                                              lockMutation.mutate({ assignmentId: a.id, isLocked: false })
                                            );
                                        }
                                      }}
                                      disabled={lockMutation.isPending}
                                      className={`inline-flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-md transition-colors disabled:opacity-50 ${
                                        allLocked
                                          ? "text-tennis-green hover:bg-tennis-green/10"
                                          : "text-gray-400 hover:bg-tennis-green/5 hover:text-tennis-green"
                                      }`}
                                    >
                                      {allLocked ? <Unlock size={14} /> : <Lock size={14} />}
                                    </button>
                                    <button
                                      type="button"
                                      title={t("offerDefinitively")}
                                      aria-label={t("offerDefinitively")}
                                      onClick={() =>
                                        setOfferTarget({
                                          ids: proposed.map((a) => a.id),
                                          name: unit.name,
                                        })
                                      }
                                      disabled={sendConfirmationMutation.isPending}
                                      className="inline-flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-md text-tennis-green transition-colors hover:bg-tennis-green/10 disabled:opacity-50"
                                    >
                                      <Mail size={14} />
                                    </button>
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            )}
          </div>

          {/* Capacity per slot — grouped by day */}
          <div className="p-4">
            <h3 className="text-sm font-semibold text-gray-900 mb-3">
              {t("capacityPerSlot")}
            </h3>
            <div className="space-y-3">
              {(() => {
                const DAY_NAMES_FULL_LOCAL = ["Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag", "Zondag"];
                const sorted = [...planning.timeSlots].sort(
                  (a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime)
                );
                const grouped: { day: number; slots: typeof sorted }[] = [];
                for (const slot of sorted) {
                  const last = grouped[grouped.length - 1];
                  if (last && last.day === slot.dayOfWeek) {
                    last.slots.push(slot);
                  } else {
                    grouped.push({ day: slot.dayOfWeek, slots: [slot] });
                  }
                }
                return grouped.map((group, gi) => (
                  <div key={group.day} className={gi > 0 ? "pt-2 border-t border-gray-100" : ""}>
                    <div className="text-[11px] font-semibold text-gray-800 mb-2">
                      {DAY_NAMES_FULL_LOCAL[group.day]}
                    </div>
                    <div className="space-y-1.5">
                      {group.slots.map((slot) => {
                        const currentCount = getSlotCurrentCount(slot.id);
                        const pct = Math.round((currentCount / slot.maxCapacity) * 100);
                        const barColor =
                          currentCount >= slot.maxCapacity
                            ? "bg-red-500"
                            : currentCount >= slot.maxCapacity * 0.75
                              ? "bg-amber-500"
                              : "bg-green-500";
                        return (
                          <div key={slot.id}>
                            <div className="flex items-center justify-between text-[10px] text-gray-600 mb-0.5">
                              <span>
                                {slot.startTime}
                                {slot.courtName && ` · ${slot.courtName}`}
                              </span>
                              <span>{currentCount}/{slot.maxCapacity}</span>
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
                ));
              })()}
            </div>
          </div>
        </aside>
      </div>

      <TimeslotDetailDialog
        readOnly={readOnly}
        open={openSlotId !== null}
        onOpenChange={(o) => !o && setOpenSlotId(null)}
        slot={
          openSlotId
            ? planning.timeSlots.find((s) => s.id === openSlotId) ?? null
            : null
        }
        assignments={openSlotId ? assignmentsBySlot.get(openSlotId) ?? [] : []}
        enrollmentMap={enrollmentMap}
        groupMap={groupMap}
        currentCount={openSlotId ? getSlotCurrentCount(openSlotId) : 0}
        onLock={(assignmentId, isLocked) =>
          lockMutation.mutate({ assignmentId, isLocked })
        }
        onOffer={(assignmentId) => sendConfirmationMutation.mutate(assignmentId)}
        onUnassign={(assignmentId) => unassignMutation.mutate(assignmentId)}
        isLockPending={lockMutation.isPending}
        isOfferPending={sendConfirmationMutation.isPending}
        isUnassignPending={unassignMutation.isPending}
        onDeleteSlot={
          openSlotId ? () => deleteSlotMutation.mutate(openSlotId) : undefined
        }
        isDeletePending={deleteSlotMutation.isPending}
        onEditSlot={() => {
          const s = openSlotId
            ? planning.timeSlots.find((x) => x.id === openSlotId)
            : null;
          if (!s) return;
          setEditingSlot({
            id: s.id,
            dayOfWeek: s.dayOfWeek,
            startTime: s.startTime,
            endTime: s.endTime,
            trainerId: s.trainerId,
            courtName: s.courtName,
            maxStudents: s.maxCapacity,
            plannedCount: getSlotCurrentCount(s.id),
          });
          setOpenSlotId(null);
        }}
        onOpenPerson={openPersonDetail}
        onOpenGroup={openGroupDetail}
      />

      {detailTarget && (
        <EnrollmentDetailDialog
          open
          onOpenChange={(o) => !o && setDetailTarget(null)}
          enrollment={detailTarget.enrollment}
          seriesId={id}
          groupMembers={detailTarget.groupMembers}
        />
      )}

      {(addingSlot || editingSlot) && (
        <AddWeekSlotDialog
          seriesId={id}
          trainers={trainers}
          editEntry={editingSlot ?? undefined}
          onClose={() => {
            setAddingSlot(false);
            setEditingSlot(null);
          }}
          onSaved={() => {
            setAddingSlot(false);
            setEditingSlot(null);
            queryClient.invalidateQueries({ queryKey: ["planning", id] });
            queryClient.invalidateQueries({ queryKey: ["lessonSeries", id] });
          }}
        />
      )}

      {/* Bevestiging vóór definitief aanbieden vanuit de Toegewezen-sectie */}
      <AlertDialog
        open={offerTarget !== null}
        onOpenChange={(open) => !open && setOfferTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("offerConfirmTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("offerConfirmBody", { name: offerTarget?.name ?? "" })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("offerConfirmCancel")}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                offerTarget?.ids.forEach((assignmentId) =>
                  sendConfirmationMutation.mutate(assignmentId)
                );
                setOfferTarget(null);
              }}
              className="bg-tennis-green hover:bg-tennis-green/90"
            >
              {t("offerConfirmButton")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

    </div>
  );
}
