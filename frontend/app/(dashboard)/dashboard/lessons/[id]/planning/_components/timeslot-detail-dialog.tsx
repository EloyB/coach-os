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
  Plus,
  UserMinus,
  Trash2,
  Pencil,
  ArrowRightLeft,
  Check,
  Clock,
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
  /** Verplaatst een bestaande toewijzing in-place naar een ander tijdslot. */
  onMove?: (assignmentId: string, slotId: string, notifyStudent: boolean) => void;
  isMovePending?: boolean;
  isLockPending: boolean;
  isOfferPending: boolean;
  isUnassignPending: boolean;
  /** Verwijdert dit weekslot uit de weektemplate (tijdelijk, o.a. voor legacy slots). */
  onDeleteSlot?: () => void;
  isDeletePending?: boolean;
  /** Opent de aanpas-dialog voor dit weekslot. */
  onEditSlot?: () => void;
  /** Slots waar deze persoon/groep nog extra aan toegevoegd kan worden (multi-slot). */
  eligibleSlotsFor?: (assignment: PlanningAssignmentDto) => ExtraSlotOption[];
  /** Wijst dezelfde persoon/groep aan een extra tijdslot toe. */
  onAssignToSlot?: (
    target: { enrollmentId?: string; groupId?: string },
    slotId: string
  ) => void;
  isAssignPending?: boolean;
  /** Klik op een persoon → open diens inschrijving-detail. */
  onOpenPerson?: (enrollmentId: string) => void;
  /** Klik op een groepsnaam → open de groep-detail. */
  onOpenGroup?: (groupId: string) => void;
}

export type ExtraSlotOption = {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courtName: string | null;
  remaining: number;
};

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
  onMove,
  isMovePending = false,
  isLockPending,
  isOfferPending,
  isUnassignPending,
  onDeleteSlot,
  isDeletePending = false,
  onEditSlot,
  eligibleSlotsFor,
  onAssignToSlot,
  isAssignPending = false,
  onOpenPerson,
  onOpenGroup,
}: TimeslotDetailDialogProps) {
  const t = useTranslations("planning");
  // Bevestiging vóór 'Definitief aanbieden': dit verstuurt meteen een e-mail-aanbod.
  const [offerTarget, setOfferTarget] = useState<{ id: string; name: string } | null>(null);
  // Welke toewijzing heeft de 'extra tijdslot'-kiezer open.
  const [addingForAssignmentId, setAddingForAssignmentId] = useState<string | null>(null);
  // Welke toewijzing heeft de 'verplaatsen'-kiezer open.
  const [movingForAssignmentId, setMovingForAssignmentId] = useState<string | null>(null);
  // Bevestiging vóór het verplaatsen van een bevestigd+betaald slot.
  const [moveConfirm, setMoveConfirm] = useState<{
    assignmentId: string;
    slot: ExtraSlotOption;
    name: string;
  } | null>(null);
  // Lesnemer mailen bij het verplaatsen? Default aan.
  const [notifyOnMove, setNotifyOnMove] = useState(true);

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
            const isConfirmed = assignment.status === "Confirmed";
            const displayName = groupName ?? names[0] ?? "";

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
                  {/* Statusbadge: toont in één oogopslag of de lesnemer al bevestigd heeft. */}
                  {isConfirmed ? (
                    <span className="inline-flex items-center gap-1 rounded bg-green-100 px-1.5 py-0.5 text-[10px] font-semibold text-green-700">
                      <Check size={10} />
                      {t("statusConfirmed")}
                    </span>
                  ) : assignment.status === "AwaitingConfirmation" ? (
                    <span className="inline-flex items-center gap-1 rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-700">
                      <Clock size={10} />
                      {t("statusOffered")}
                    </span>
                  ) : (
                    <span className="rounded bg-gray-100 px-1.5 py-0.5 text-[10px] font-medium text-gray-500">
                      {t("statusDraft")}
                    </span>
                  )}
                  {/* Lock enkel tonen bij concept: bij bevestigd/aangeboden is het impliciet. */}
                  {assignment.isLocked && assignment.status === "Proposed" && (
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

                {/* Actions — compacte icoon-balk, tooltip bij hover.
                    Bevestigd = niet verwijderbaar (betaald), dus enkel tonen als er
                    een actie is (aanbieden/vastzetten of verwijderen). */}
                {!readOnly && (canOffer || !isConfirmed) && (
                  <div className="mt-3 flex items-center justify-end gap-1 border-t border-gray-100 pt-3">
                    {canOffer && (
                      <>
                        <button
                          type="button"
                          title={
                            assignment.isLocked
                              ? t("unlock")
                              : assignment.groupId
                                ? t("lockGroup")
                                : t("lock")
                          }
                          aria-label={assignment.isLocked ? t("unlock") : t("lock")}
                          onClick={() => onLock(assignment.id, assignment.isLocked)}
                          disabled={isLockPending}
                          className={`inline-flex h-8 w-8 shrink-0 cursor-pointer items-center justify-center rounded-md transition-colors disabled:opacity-50 ${
                            assignment.isLocked
                              ? "text-tennis-green hover:bg-tennis-green/10"
                              : "text-gray-400 hover:bg-tennis-green/5 hover:text-tennis-green"
                          }`}
                        >
                          {assignment.isLocked ? (
                            <Unlock size={15} />
                          ) : (
                            <Lock size={15} />
                          )}
                        </button>
                        <button
                          type="button"
                          title={t("offerDefinitively")}
                          aria-label={t("offerDefinitively")}
                          onClick={() =>
                            setOfferTarget({
                              id: assignment.id,
                              name: groupName ?? names[0] ?? "",
                            })
                          }
                          disabled={isOfferPending}
                          className="inline-flex h-8 w-8 shrink-0 cursor-pointer items-center justify-center rounded-md text-tennis-green transition-colors hover:bg-tennis-green/10 disabled:opacity-50"
                        >
                          <Mail size={15} />
                        </button>
                      </>
                    )}
                    {/* Bevestigde toewijzing niet verwijderbaar — enkel verplaatsen. */}
                    {!isConfirmed && (
                      <button
                        type="button"
                        title={t("unassign")}
                        aria-label={t("unassign")}
                        onClick={() => onUnassign(assignment.id)}
                        disabled={isUnassignPending}
                        className="inline-flex h-8 w-8 shrink-0 cursor-pointer items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
                      >
                        <UserMinus size={15} />
                      </button>
                    )}
                  </div>
                )}

                {/* Acties: Verplaatsen (bevestigd) + Extra tijdslot — naast elkaar,
                    visueel onderscheiden (omlijnde knop vs tekstlink). */}
                {!readOnly && (() => {
                  const target = assignment.groupId
                    ? { groupId: assignment.groupId }
                    : assignment.enrollmentId
                      ? { enrollmentId: assignment.enrollmentId }
                      : null;
                  const showMove = isConfirmed && onMove !== undefined;
                  const showExtra = onAssignToSlot !== undefined && target !== null;
                  if (!showMove && !showExtra) return null;

                  const options = eligibleSlotsFor?.(assignment) ?? [];
                  const moveOpen = movingForAssignmentId === assignment.id;
                  const extraOpen = addingForAssignmentId === assignment.id;

                  return (
                    <div className="mt-2 border-t border-gray-100 pt-2">
                      {/* Triggerrij (verborgen zodra een kiezer open is) */}
                      {!moveOpen && !extraOpen && (
                        <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                          {showMove && (
                            <button
                              type="button"
                              onClick={() => setMovingForAssignmentId(assignment.id)}
                              className="inline-flex cursor-pointer items-center gap-1 rounded-md border border-tennis-green/40 px-2 py-1 text-[11px] font-semibold text-tennis-green transition-colors hover:bg-tennis-green/5"
                            >
                              <ArrowRightLeft size={12} />
                              {t("moveAssignment")}
                            </button>
                          )}
                          {showExtra && (
                            <button
                              type="button"
                              onClick={() => setAddingForAssignmentId(assignment.id)}
                              className="inline-flex cursor-pointer items-center gap-1 text-[11px] font-medium text-gray-500 transition-colors hover:text-tennis-green hover:underline"
                            >
                              <Plus size={12} />
                              {t("addExtraSlot")}
                            </button>
                          )}
                        </div>
                      )}

                      {/* Verplaats-kiezer */}
                      {moveOpen && (
                        <div className="space-y-1.5">
                          <p className="text-[11px] font-medium text-gray-500">
                            {t("chooseMoveSlot")}
                          </p>
                          {options.length === 0 ? (
                            <p className="text-[11px] text-gray-400">
                              {t("noOtherSlotAvailable")}
                            </p>
                          ) : (
                            <div className="space-y-1">
                              {options.map((s) => (
                                <button
                                  key={s.id}
                                  type="button"
                                  disabled={isMovePending}
                                  onClick={() => {
                                    setNotifyOnMove(true);
                                    setMoveConfirm({
                                      assignmentId: assignment.id,
                                      slot: s,
                                      name: displayName,
                                    });
                                    setMovingForAssignmentId(null);
                                  }}
                                  className="w-full cursor-pointer rounded-md border border-gray-200 px-2 py-1.5 text-left text-[11px] text-gray-700 transition-colors hover:border-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
                                >
                                  <span className="font-medium">
                                    {DAY_NAMES_FULL[s.dayOfWeek]} {s.startTime}–{s.endTime}
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
                            onClick={() => setMovingForAssignmentId(null)}
                            className="cursor-pointer text-[11px] text-gray-400 hover:text-gray-600"
                          >
                            {t("cancel")}
                          </button>
                        </div>
                      )}

                      {/* Extra-tijdslot-kiezer */}
                      {extraOpen && showExtra && (
                        <div className="space-y-1.5">
                          <p className="text-[11px] font-medium text-gray-500">
                            {t("chooseExtraSlot")}
                          </p>
                          {options.length === 0 ? (
                            <p className="text-[11px] text-gray-400">
                              {t("noOtherSlotAvailable")}
                            </p>
                          ) : (
                            <div className="space-y-1">
                              {options.map((s) => (
                                <button
                                  key={s.id}
                                  type="button"
                                  disabled={isAssignPending}
                                  onClick={() => {
                                    onAssignToSlot!(target!, s.id);
                                    setAddingForAssignmentId(null);
                                  }}
                                  className="w-full cursor-pointer rounded-md border border-gray-200 px-2 py-1.5 text-left text-[11px] text-gray-700 transition-colors hover:border-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
                                >
                                  <span className="font-medium">
                                    {DAY_NAMES_FULL[s.dayOfWeek]} {s.startTime}–{s.endTime}
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
                            onClick={() => setAddingForAssignmentId(null)}
                            className="cursor-pointer text-[11px] text-gray-400 hover:text-gray-600"
                          >
                            {t("cancel")}
                          </button>
                        </div>
                      )}
                    </div>
                  );
                })()}
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
      open={moveConfirm !== null}
      onOpenChange={(open) => !open && setMoveConfirm(null)}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("moveConfirmTitle")}</AlertDialogTitle>
          <AlertDialogDescription>
            {t("moveConfirmBody", {
              name: moveConfirm?.name ?? "",
              day: moveConfirm ? DAY_NAMES_FULL[moveConfirm.slot.dayOfWeek] : "",
              start: moveConfirm?.slot.startTime ?? "",
              end: moveConfirm?.slot.endTime ?? "",
            })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <label className="flex cursor-pointer items-start gap-2.5 rounded-lg border border-gray-200 bg-gray-50/50 px-3 py-2.5 text-sm text-gray-700">
          <input
            type="checkbox"
            checked={notifyOnMove}
            onChange={(e) => setNotifyOnMove(e.target.checked)}
            className="mt-0.5 h-4 w-4 shrink-0 accent-tennis-green"
          />
          <span>{t("moveNotifyLabel")}</span>
        </label>
        <AlertDialogFooter>
          <AlertDialogCancel>{t("moveConfirmCancel")}</AlertDialogCancel>
          <AlertDialogAction
            onClick={() => {
              if (moveConfirm)
                onMove?.(moveConfirm.assignmentId, moveConfirm.slot.id, notifyOnMove);
              setMoveConfirm(null);
            }}
            className="bg-tennis-green hover:bg-tennis-green/90"
          >
            {t("moveConfirmButton")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>

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
