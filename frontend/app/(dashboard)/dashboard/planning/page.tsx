"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { CalendarDays, Clock3, MapPin, Users } from "lucide-react";
import { useTranslations } from "next-intl";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
    <Card>
      <CardHeader className="flex-row items-start justify-between gap-4 space-y-0">
        <div>
          <CardTitle className="text-lg">{planning.lessonSerieName}</CardTitle>
          <p className="mt-1 text-sm text-muted-foreground">
            {getStatusLabel(planning.planningStatus, t)} · {planning.enrollments.length} {t("students")}
          </p>
        </div>
        <CalendarDays className="h-5 w-5 text-tennis-green" aria-hidden="true" />
      </CardHeader>
      <CardContent>
        {slotRows.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t("noSlots")}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[620px] text-sm">
              <thead>
                <tr className="border-b text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="pb-3 pr-4 font-medium">{t("day")}</th>
                  <th className="pb-3 pr-4 font-medium">{t("time")}</th>
                  <th className="pb-3 pr-4 font-medium">{t("court")}</th>
                  <th className="pb-3 font-medium">{t("studentsColumn")}</th>
                </tr>
              </thead>
              <tbody>
                {slotRows.map((slot) => {
                  const students = (assignmentsBySlot.get(slot.id) ?? [])
                    .flatMap((assignment) => getStudentsForAssignment(assignment, planning));

                  return (
                    <tr key={slot.id} className="border-b last:border-0">
                      <td className="py-3 pr-4 font-medium">{t(`day${slot.dayOfWeek}`)}</td>
                      <td className="whitespace-nowrap py-3 pr-4">
                        <span className="inline-flex items-center gap-1.5">
                          <Clock3 className="h-3.5 w-3.5 text-muted-foreground" aria-hidden="true" />
                          {slot.startTime} – {slot.endTime}
                        </span>
                      </td>
                      <td className="py-3 pr-4">
                        {slot.courtName ? (
                          <span className="inline-flex items-center gap-1.5">
                            <MapPin className="h-3.5 w-3.5 text-muted-foreground" aria-hidden="true" />
                            {slot.courtName}
                          </span>
                        ) : (
                          <span className="text-muted-foreground">{t("noCourt")}</span>
                        )}
                      </td>
                      <td className="py-3">
                        {students.length > 0 ? (
                          <span className="inline-flex items-center gap-1.5">
                            <Users className="h-3.5 w-3.5 text-muted-foreground" aria-hidden="true" />
                            {students.join(", ")}
                          </span>
                        ) : (
                          <span className="text-muted-foreground">{t("noStudents")}</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default function PlanningOverviewPage() {
  const t = useTranslations("trainerPlanning");
  const { data: plannings = [], isLoading, isError } = useQuery({
    queryKey: ["trainerPlanning"],
    queryFn: getTrainerPlanning,
  });

  return (
    <main className="space-y-6 p-6 pb-24 lg:p-8">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.16em] text-tennis-green">{t("eyebrow")}</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">{t("description")}</p>
      </header>

      {isLoading && <p className="text-sm text-muted-foreground">{t("loading")}</p>}
      {isError && <p className="text-sm text-destructive">{t("error")}</p>}
      {!isLoading && !isError && plannings.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-sm text-muted-foreground">{t("empty")}</CardContent>
        </Card>
      )}
      {!isLoading && !isError && plannings.length > 0 && (
        <div className="space-y-5">
          {plannings.map((planning) => (
            <SeriesPlanning key={planning.lessonSerieId} planning={planning} t={t} />
          ))}
        </div>
      )}
    </main>
  );
}
