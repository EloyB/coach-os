"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { CalendarDays, Clock3, MapPin, Users } from "lucide-react";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import {
  getTrainerPlanning,
  type PlanningAssignmentDto,
  type TrainerPlanningDto,
} from "@/lib/api/planning";

function getStudentsForAssignment(
  assignment: PlanningAssignmentDto,
  planning: TrainerPlanningDto,
): string[] {
  const enrollments = new Map(planning.enrollments.map((enrollment) => [enrollment.id, enrollment]));
  const groups = new Map(planning.groups.map((group) => [group.id, group]));

  if (assignment.enrollmentId) {
    const enrollment = enrollments.get(assignment.enrollmentId);
    return enrollment ? [enrollment.studentName] : [];
  }

  if (assignment.groupId) {
    const group = groups.get(assignment.groupId);
    return group?.memberEnrollmentIds
      .map((id) => enrollments.get(id)?.studentName)
      .filter((name): name is string => Boolean(name)) ?? [];
  }

  return [];
}

function getStatusLabel(status: string, t: (key: string) => string): string {
  switch (status) {
    case "Enrollment":
      return t("statusEnrollment");
    case "Planning":
      return t("statusPlanning");
    case "AwaitingConfirmation":
      return t("statusAwaitingConfirmation");
    case "Scheduled":
      return t("statusScheduled");
    default:
      return status;
  }
}

function SeriesPlanning({ planning, t }: { planning: TrainerPlanningDto; t: (key: string) => string }) {
  const assignmentsBySlot = useMemo(() => {
    const map = new Map<string, PlanningAssignmentDto[]>();
    for (const assignment of planning.assignments) {
      const assignments = map.get(assignment.timeSlotId) ?? [];
      assignments.push(assignment);
      map.set(assignment.timeSlotId, assignments);
    }
    return map;
  }, [planning.assignments]);

  const slotRows = [...planning.timeSlots].sort(
    (a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime),
  );

  return (
    <section className="bg-paper border border-rule rounded-xl overflow-hidden">
      <div className="flex items-start justify-between gap-4 px-4 py-3.5 border-b border-rule bg-[#fbfaf6]">
        <div>
          <h2 className="text-[12.5px] font-bold text-ink tracking-tight">{planning.lessonSerieName}</h2>
          <Mono className="text-[10.5px] text-ink-3 mt-0.5 block">
            {getStatusLabel(planning.planningStatus, t)} · {planning.enrollments.length} {t("students")}
          </Mono>
        </div>
        <CalendarDays size={15} className="text-tennis-green mt-0.5" aria-hidden="true" />
      </div>

      {slotRows.length === 0 ? (
        <p className="px-4 py-5 text-[12px] text-ink-3">{t("noSlots")}</p>
      ) : (
        <div className="overflow-x-auto">
          <div className="min-w-[620px]">
            <div className="grid grid-cols-[.9fr_1.1fr_1fr_2fr] gap-3 px-4 py-2.5 text-[10px] text-ink-3 font-semibold font-mono uppercase tracking-[0.08em] border-b border-rule">
              <span>{t("day")}</span>
              <span>{t("time")}</span>
              <span>{t("court")}</span>
              <span>{t("studentsColumn")}</span>
            </div>
            {slotRows.map((slot) => {
              const students = (assignmentsBySlot.get(slot.id) ?? [])
                .flatMap((assignment) => getStudentsForAssignment(assignment, planning));

              return (
                <div
                  key={slot.id}
                  className="grid grid-cols-[.9fr_1.1fr_1fr_2fr] gap-3 px-4 py-3 items-center border-b border-rule last:border-b-0 text-[11.5px] hover:bg-canvas/50 transition-colors"
                >
                  <span className="font-semibold text-ink">{t(`day${slot.dayOfWeek}`)}</span>
                  <Mono className="inline-flex items-center gap-1.5 text-ink-2 text-[11px]">
                    <Clock3 size={12} className="text-ink-3" aria-hidden="true" />
                    {slot.startTime} – {slot.endTime}
                  </Mono>
                  {slot.courtName ? (
                    <span className="inline-flex items-center gap-1.5 text-ink-2">
                      <MapPin size={12} className="text-ink-3" aria-hidden="true" />
                      {slot.courtName}
                    </span>
                  ) : (
                    <span className="text-ink-3">{t("noCourt")}</span>
                  )}
                  {students.length > 0 ? (
                    <span className="inline-flex items-center gap-1.5 text-ink">
                      <Users size={12} className="text-ink-3 shrink-0" aria-hidden="true" />
                      <span className="truncate">{students.join(", ")}</span>
                    </span>
                  ) : (
                    <span className="text-ink-3">{t("noStudents")}</span>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </section>
  );
}

export default function PlanningOverviewPage() {
  const t = useTranslations("trainerPlanning");
  const { data: plannings = [], isLoading, isError } = useQuery({
    queryKey: ["trainerPlanning"],
    queryFn: getTrainerPlanning,
  });

  return (
    <>
      <div className="flex items-center justify-between mb-5">
        <div>
          <SlashLabel>{t("eyebrow")}</SlashLabel>
          <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">{t("title")}</h1>
          <p className="text-[11.5px] text-ink-3 mt-1 max-w-2xl">{t("description")}</p>
        </div>
        <span className="hidden sm:inline-flex items-center gap-1.5 text-[10.5px] px-2 py-0.5 rounded-full bg-tennis-green/10 text-tennis-green font-semibold">
          <span className="w-1.5 h-1.5 rounded-full bg-tennis-green" aria-hidden="true" />
          {t("readOnly")}
        </span>
      </div>

      {isLoading && (
        <div className="bg-paper border border-rule rounded-xl overflow-hidden animate-pulse">
          <div className="h-11 bg-canvas border-b border-rule" />
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-12 border-b border-rule" />
          ))}
        </div>
      )}
      {isError && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-[12px] text-red-600">{t("error")}</div>
      )}
      {!isLoading && !isError && plannings.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 px-6 text-center">
          <div className="w-16 h-16 rounded-full bg-tennis-green/[.08] flex items-center justify-center mb-5">
            <CalendarDays className="w-8 h-8 text-tennis-green" aria-hidden="true" />
          </div>
          <p className="text-ink font-semibold text-sm mb-1">{t("empty")}</p>
          <p className="text-ink-3 text-[12px] max-w-64 leading-relaxed">{t("emptyDescription")}</p>
        </div>
      )}
      {!isLoading && !isError && plannings.length > 0 && (
        <div className="space-y-[18px]">
          {plannings.map((planning) => (
            <SeriesPlanning key={planning.lessonSerieId} planning={planning} t={t} />
          ))}
        </div>
      )}
    </>
  );
}
