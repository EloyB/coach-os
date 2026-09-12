"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Users,
  User,
  Mail,
  Lock,
  Unlock,
  X,
  UserMinus,
  Trash2,
  Pencil,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
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
  /** Verwijdert dit weekslot uit de weektemplate (tijdelijk, o.a. voor legacy slots). */
  onDeleteSlot?: () => void;
  isDeletePending?: boolean;
  /** Opent de aanpas-dialog voor dit weekslot. */
  onEditSlot?: () => void;
  /** Klik op een persoon → open diens inschrijving-detail. */
  onOpenPerson?: (enrollmentId: string) => void;
  /** Klik op een groepsnaam → open de groep-detail. */
  onOpenGroup?: (groupId: string) => void;
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
  onDeleteSlot,
  isDeletePending = false,
  onEditSlot,
  onOpenPerson,
  onOpenGroup,
}: TimeslotDetailDialogProps) {
  const t = useTranslations("planning");
  // Bevestiging vóór 'Definitief aanbieden': dit verstuurt meteen een e-mail-aanbod.
  const [offerTarget, setOfferTarget] = useState<{ id: string; name: string } | null>(null);

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
    <>
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="flex max-h-[85vh] max-w-md flex-col gap-0 overflow-hidden p-0"
        aria-describedby={undefined}
        showCloseButton={false}
      >
        {/* Sticky header: titel links, acties (⋮ + custom sluit-X) rechts — samen uitgelijnd */}
        <DialogHeader className="shrink-0 border-b border-gray-100 px-6 pb-3 pt-5">
          <div className="flex items-center justify-between gap-2">
            <DialogTitle>
              {DAY_NAMES_FULL[slot.dayOfWeek]} {slot.startTime}–{slot.endTime}
            </DialogTitle>
            <div className="flex shrink-0 items-center gap-0.5">
              {!readOnly && onEditSlot && (
                <button
                  type="button"
                  aria-label={t("editSlot")}
                  title={t("editSlot")}
                  onClick={onEditSlot}
                  className="flex h-8 w-8 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600 focus:outline-none"
                >
                  <Pencil size={17} />
                </button>
              )}
              {!readOnly && onDeleteSlot && (
                <button
                  type="button"
                  aria-label={t("deleteSlot")}
                  title={t("deleteSlot")}
                  onClick={onDeleteSlot}
                  disabled={isDeletePending}
                  className="flex h-8 w-8 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-red-50 hover:text-red-600 focus:outline-none disabled:opacity-50"
                >
                  <Trash2 size={17} />
                </button>
              )}
              <button
                type="button"
                aria-label={t("close")}
                onClick={() => onOpenChange(false)}
                className="flex h-8 w-8 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600 focus:outline-none"
              >
                <X size={18} />
              </button>
            </div>
          </div>
        </DialogHeader>

        {/* Scrollbaar deel */}
        <div className="flex min-h-0 flex-1 flex-col overflow-y-auto px-6 pt-3 pb-2">
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
            const people: { name: string; enrollmentId: string }[] = [];
            let groupName: string | null = null;

            if (assignment.groupId) {
              const group = groupMap.get(assignment.groupId);
              if (group) {
                groupName = group.name;
                for (const mId of group.memberEnrollmentIds) {
                  const e = enrollmentMap.get(mId);
                  if (e) people.push({ name: e.studentName, enrollmentId: e.id });
                }
              }
            } else if (assignment.enrollmentId) {
              const e = enrollmentMap.get(assignment.enrollmentId);
              if (e) people.push({ name: e.studentName, enrollmentId: e.id });
            }

            if (people.length === 0) return null;
            const names = people.map((p) => p.name);

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
                      <button
                        type="button"
                        onClick={() =>
                          assignment.groupId && onOpenGroup?.(assignment.groupId)
                        }
                        className={`cursor-pointer rounded px-1.5 py-0.5 text-[11px] font-bold transition-colors hover:underline ${
                          assignment.isAutoMerged
                            ? "bg-blue-100 text-blue-700 hover:bg-blue-200"
                            : "bg-green-100 text-green-700 hover:bg-green-200"
                        }`}
                      >
                        {groupName}
                      </button>
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
                </div>

                {/* Members */}
                <div className="space-y-1.5 pl-1">
                  {people.map((person, ni) => {
                    const color = getAvatarColor(person.name);
                    return (
                      <button
                        key={ni}
                        type="button"
                        onClick={() => onOpenPerson?.(person.enrollmentId)}
                        className="group flex w-full cursor-pointer items-center gap-2 rounded-md px-1.5 py-1 text-left transition-colors hover:bg-tennis-green/10"
                      >
                        <div
                          className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[8px] font-bold ${color.bg} ${color.text}`}
                        >
                          {getInitials(person.name)}
                        </div>
                        <span className="text-sm text-gray-700 group-hover:text-tennis-green group-hover:underline">
                          {person.name}
                        </span>
                      </button>
                    );
                  })}
                </div>

                {/* Actions */}
                {!readOnly && (
                  <div className="mt-3 flex items-center gap-2 border-t border-gray-100 pt-3">
                    {canOffer && (
                      <>
                        <button
                          type="button"
                          onClick={() => onLock(assignment.id, assignment.isLocked)}
                          disabled={isLockPending}
                          className={`inline-flex flex-1 cursor-pointer items-center justify-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-semibold transition-colors disabled:opacity-50 ${
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
                          onClick={() =>
                            setOfferTarget({
                              id: assignment.id,
                              name: groupName ?? names[0] ?? "",
                            })
                          }
                          disabled={isOfferPending}
                          className="inline-flex flex-1 cursor-pointer items-center justify-center gap-1.5 rounded-md bg-tennis-green px-2.5 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-tennis-green/90 disabled:opacity-50"
                        >
                          <Mail size={12} />
                          {t("offerDefinitively")}
                        </button>
                      </>
                    )}
                    <button
                      type="button"
                      title={t("unassign")}
                      aria-label={t("unassign")}
                      onClick={() => onUnassign(assignment.id)}
                      disabled={isUnassignPending}
                      className="inline-flex h-8 w-8 shrink-0 cursor-pointer items-center justify-center rounded-md border border-gray-200 text-gray-400 transition-colors hover:border-red-200 hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
                    >
                      <UserMinus size={15} />
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        </div>

        {/* Footer (sticky onderaan; scrollt niet mee) */}
        <div className="flex shrink-0 items-center justify-end border-t border-gray-100 bg-background px-6 py-4">
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
              if (offerTarget) onOffer(offerTarget.id);
              setOfferTarget(null);
            }}
            className="bg-tennis-green hover:bg-tennis-green/90"
          >
            {t("offerConfirmButton")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
    </>
  );
}
