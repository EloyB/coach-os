"use client";

import { useTranslations } from "next-intl";
import { Users, User, Mail, Lock, Unlock, X } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { getInitials, getAvatarColor } from "@/lib/planning-avatars";
import type {
  PlanningTimeSlotDto,
  PlanningEnrollmentDto,
  PlanningGroupDto,
  PlanningAssignmentDto,
} from "@/lib/api/planning";

const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

interface TimeslotDetailDialogProps {
  /** Hoofdtrainer = read-only: enkel bekijken, geen lock/aanbieden/verwijderen. */
  readOnly?: boolean;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  slot: PlanningTimeSlotDto | null;
  assignments: PlanningAssignmentDto[];
  enrollmentMap: Map<string, PlanningEnrollmentDto>;
  groupMap: Map<string, PlanningGroupDto>;
  currentCount: number;
  onLock: (assignmentId: string, isLocked: boolean) => void;
  onOffer: (assignmentId: string) => void;
  onUnassign: (assignmentId: string) => void;
  isLockPending: boolean;
  isOfferPending: boolean;
  isUnassignPending: boolean;
}

export function TimeslotDetailDialog({
  readOnly = false,
  open,
  onOpenChange,
  slot,
  assignments,
  enrollmentMap,
  groupMap,
  currentCount,
  onLock,
  onOffer,
  onUnassign,
  isLockPending,
  isOfferPending,
  isUnassignPending,
}: TimeslotDetailDialogProps) {
  const t = useTranslations("planning");

  if (!slot) return null;

  const subtitle =
    [slot.courtName, slot.trainerName].filter(Boolean).join(" · ") || null;

  const countColor =
    currentCount >= slot.maxCapacity
      ? "text-red-600"
      : currentCount >= slot.maxCapacity * 0.75
        ? "text-amber-600"
        : "text-green-600";

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle>
            {DAY_NAMES_FULL[slot.dayOfWeek]} {slot.startTime}–{slot.endTime}
          </DialogTitle>
        </DialogHeader>

        {/* Slot meta */}
        <div className="mt-1 flex items-center justify-between gap-3">
          <span className="text-sm text-gray-500">{subtitle ?? " "}</span>
          <span className={`text-sm font-medium shrink-0 ${countColor}`}>
            {t("occupied", { count: currentCount, max: slot.maxCapacity })}
          </span>
        </div>

        {/* Assignments */}
        <div className="mt-3 space-y-3">
          {assignments.length === 0 && (
            <p className="rounded-lg border border-dashed border-gray-200 px-4 py-6 text-center text-sm text-gray-400">
              {t("slotDialogEmpty")}
            </p>
          )}

          {assignments.map((assignment) => {
            const names: string[] = [];
            let groupName: string | null = null;

            if (assignment.groupId) {
              const group = groupMap.get(assignment.groupId);
              if (group) {
                groupName = group.name;
                for (const mId of group.memberEnrollmentIds) {
                  const e = enrollmentMap.get(mId);
                  if (e) names.push(e.studentName);
                }
              }
            } else if (assignment.enrollmentId) {
              const e = enrollmentMap.get(assignment.enrollmentId);
              if (e) names.push(e.studentName);
            }

            if (names.length === 0) return null;

            const canOffer = assignment.status === "Proposed";

            return (
              <div
                key={assignment.id}
                className={`rounded-lg border p-3 ${
                  assignment.isLocked
                    ? "border-tennis-green bg-green-50/50"
                    : assignment.isAutoMerged
                      ? "border-blue-200 bg-blue-50/30"
                      : "border-gray-200"
                }`}
              >
                {/* Assignment header */}
                <div className="mb-2 flex items-center gap-1.5">
                  {groupName ? (
                    <>
                      <Users size={13} className="shrink-0 text-gray-400" />
                      <span
                        className={`rounded px-1.5 py-0.5 text-[11px] font-bold ${
                          assignment.isAutoMerged
                            ? "bg-blue-100 text-blue-700"
                            : "bg-green-100 text-green-700"
                        }`}
                      >
                        {groupName}
                      </span>
                    </>
                  ) : (
                    <>
                      <User size={13} className="shrink-0 text-gray-400" />
                      <span className="text-xs text-gray-500">Individueel</span>
                    </>
                  )}
                  {assignment.isAutoMerged && (
                    <span className="text-[10px] italic text-blue-500">auto</span>
                  )}
                  {assignment.isLocked && (
                    <span className="inline-flex items-center gap-1 rounded bg-green-100 px-1.5 py-0.5 text-[10px] font-semibold text-green-700">
                      <Lock size={10} />
                      {t("locked")}
                    </span>
                  )}
                  {!readOnly && (
                    <button
                      type="button"
                      title={t("unassign")}
                      onClick={() => onUnassign(assignment.id)}
                      disabled={isUnassignPending}
                      className="ml-auto flex h-6 w-6 shrink-0 items-center justify-center rounded text-gray-300 transition-colors hover:bg-red-50 hover:text-red-500 disabled:opacity-50"
                    >
                      <X size={14} />
                    </button>
                  )}
                </div>

                {/* Members */}
                <div className="space-y-1.5 pl-1">
                  {names.map((name, ni) => {
                    const color = getAvatarColor(name);
                    return (
                      <div key={ni} className="flex items-center gap-2">
                        <div
                          className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[8px] font-bold ${color.bg} ${color.text}`}
                        >
                          {getInitials(name)}
                        </div>
                        <span className="text-sm text-gray-700">{name}</span>
                      </div>
                    );
                  })}
                </div>

                {/* Actions */}
                {!readOnly && canOffer && (
                  <div className="mt-3 flex items-center gap-2 border-t border-gray-100 pt-3">
                    <button
                      type="button"
                      onClick={() => onLock(assignment.id, assignment.isLocked)}
                      disabled={isLockPending}
                      className={`inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-semibold transition-colors disabled:opacity-50 ${
                        assignment.isLocked
                          ? "bg-green-100 text-green-700 hover:bg-green-200"
                          : "border border-gray-200 text-tennis-green hover:bg-tennis-green/5"
                      }`}
                    >
                      {assignment.isLocked ? (
                        <Unlock size={12} />
                      ) : (
                        <Lock size={12} />
                      )}
                      {assignment.isLocked
                        ? t("unlock")
                        : assignment.groupId
                          ? t("lockGroup")
                          : t("lock")}
                    </button>
                    <button
                      type="button"
                      onClick={() => onOffer(assignment.id)}
                      disabled={isOfferPending}
                      className="inline-flex items-center gap-1.5 rounded-md bg-tennis-green px-2.5 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-tennis-green/90 disabled:opacity-50"
                    >
                      <Mail size={12} />
                      {t("offerDefinitively")}
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Footer */}
        <div className="mt-4 flex justify-end border-t border-gray-100 pt-4">
          <button
            type="button"
            onClick={() => onOpenChange(false)}
            className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 transition-colors hover:bg-gray-50"
          >
            {t("close")}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
