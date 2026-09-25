"use client";

import { useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import {
  Users,
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
  MoreVertical,
  ChevronRight,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
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

function MenuItem({
  icon,
  children,
  onClick,
  disabled,
  danger,
}: {
  icon: ReactNode;
  children: ReactNode;
  onClick: () => void;
  disabled?: boolean;
  danger?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`flex w-full cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-left text-xs font-medium transition-colors disabled:opacity-50 ${
        danger
          ? "text-red-600 hover:bg-red-50"
          : "text-gray-600 hover:bg-tennis-green/5 hover:text-tennis-green"
      }`}
    >
      <span className="shrink-0">{icon}</span>
      {children}
    </button>
  );
}

/** ⋮-menu met de acties voor een toewijzing (individueel of groep). */
function AssignmentActionsMenu({
  name,
  isLocked,
  canOffer,
  showMove,
  showExtra,
  showUnassign,
  onLock,
  onOffer,
  onMove,
  onExtra,
  onUnassign,
  lockPending,
  offerPending,
  unassignPending,
}: {
  name: string;
  isLocked: boolean;
  canOffer: boolean;
  showMove: boolean;
  showExtra: boolean;
  showUnassign: boolean;
  onLock: () => void;
  onOffer: () => void;
  onMove: () => void;
  onExtra: () => void;
  onUnassign: () => void;
  lockPending?: boolean;
  offerPending?: boolean;
  unassignPending?: boolean;
}) {
  const t = useTranslations("planning");
  const [open, setOpen] = useState(false);
  const close = () => setOpen(false);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={t("actionsFor", { name })}
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700"
        >
          <MoreVertical size={16} />
        </button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-52 p-1.5 text-sm">
        <div className="space-y-0.5">
          {canOffer && (
            <MenuItem
              icon={isLocked ? <Unlock size={14} /> : <Lock size={14} />}
              onClick={() => {
                onLock();
                close();
              }}
              disabled={lockPending}
            >
              {isLocked ? t("unlock") : t("lock")}
            </MenuItem>
          )}
          {canOffer && (
            <MenuItem
              icon={<Mail size={14} />}
              onClick={() => {
                onOffer();
                close();
              }}
              disabled={offerPending}
            >
              {t("offerDefinitively")}
            </MenuItem>
          )}
          {showMove && (
            <MenuItem
              icon={<ArrowRightLeft size={14} />}
              onClick={() => {
                onMove();
                close();
              }}
            >
              {t("moveAssignment")}
            </MenuItem>
          )}
          {showExtra && (
            <MenuItem
              icon={<Plus size={14} />}
              onClick={() => {
                onExtra();
                close();
              }}
            >
              {t("addExtraSlot")}
            </MenuItem>
          )}
          {showUnassign && (
            <MenuItem
              icon={<UserMinus size={14} />}
              danger
              onClick={() => {
                onUnassign();
                close();
              }}
              disabled={unassignPending}
            >
              {t("unassign")}
            </MenuItem>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

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
  // Bevestiging vóór het verwijderen van een tijdslot (extra safety).
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);

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
                  onClick={() => setConfirmDeleteOpen(true)}
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

            const target = assignment.groupId
              ? { groupId: assignment.groupId }
              : assignment.enrollmentId
                ? { enrollmentId: assignment.enrollmentId }
                : null;
            const showMove = isConfirmed && onMove !== undefined;
            const showExtra = onAssignToSlot !== undefined && target !== null;
            const showUnassign = !isConfirmed;
            const options = eligibleSlotsFor?.(assignment) ?? [];
            const moveOpen = movingForAssignmentId === assignment.id;
            const extraOpen = addingForAssignmentId === assignment.id;

            const statusBadge = isConfirmed ? (
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
            );

            const movePicker = moveOpen && (
              <div className="mt-3 space-y-1.5 border-t border-gray-100 pt-3">
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
            );

            const extraPicker = extraOpen && showExtra && (
              <div className="mt-3 space-y-1.5 border-t border-gray-100 pt-3">
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
            );

            // Individueel: alles op één regel — avatar + naam links, status +
            // acties (onder een ⋮-menu) rechts.
            if (!groupName) {
              const person = people[0];
              const color = getAvatarColor(person.name);
              const hasActions =
                !readOnly &&
                (canOffer || showMove || showExtra || showUnassign);

              return (
                <div
                  key={assignment.id}
                  className={`group relative rounded-lg border p-3 ${
                    assignment.isLocked
                      ? "border-tennis-green bg-green-50/50"
                      : "border-gray-200"
                  }`}
                >
                  {/* Onzichtbare knop over de hele card opent het speler-detail;
                      het ⋮-menu vangt zijn eigen clicks af via pointer-events.
                      Bij een open kiezer laten we de overlay weg zodat die klikbaar
                      blijft. */}
                  {!moveOpen && !extraOpen && (
                    <button
                      type="button"
                      aria-label={person.name}
                      onClick={() => onOpenPerson?.(person.enrollmentId)}
                      className="absolute inset-0 z-0 cursor-pointer rounded-lg transition-colors hover:bg-gray-50"
                    />
                  )}
                  <div className="pointer-events-none relative z-10 flex items-center justify-between gap-2">
                    <div className="flex min-w-0 flex-1 items-center gap-2">
                      <div
                        className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-[9px] font-bold ${color.bg} ${color.text}`}
                      >
                        {getInitials(person.name)}
                      </div>
                      <span className="truncate text-sm text-gray-700">
                        {person.name}
                      </span>
                    </div>
                    <div className="flex shrink-0 items-center gap-1.5">
                      {statusBadge}
                      {assignment.isLocked &&
                        assignment.status === "Proposed" && (
                          <span className="inline-flex items-center gap-1 rounded bg-green-100 px-1.5 py-0.5 text-[10px] font-semibold text-green-700">
                            <Lock size={10} />
                            {t("locked")}
                          </span>
                        )}
                      {hasActions && !moveOpen && !extraOpen && (
                        <span className="pointer-events-auto">
                          <AssignmentActionsMenu
                            name={person.name}
                            isLocked={assignment.isLocked}
                            canOffer={canOffer}
                            showMove={showMove}
                            showExtra={showExtra}
                            showUnassign={showUnassign}
                            onLock={() =>
                              onLock(assignment.id, assignment.isLocked)
                            }
                            onOffer={() =>
                              setOfferTarget({
                                id: assignment.id,
                                name: person.name,
                              })
                            }
                            onMove={() =>
                              setMovingForAssignmentId(assignment.id)
                            }
                            onExtra={() =>
                              setAddingForAssignmentId(assignment.id)
                            }
                            onUnassign={() => onUnassign(assignment.id)}
                            lockPending={isLockPending}
                            offerPending={isOfferPending}
                            unassignPending={isUnassignPending}
                          />
                        </span>
                      )}
                      {!moveOpen && !extraOpen && (
                        <ChevronRight
                          size={16}
                          className="shrink-0 text-gray-300"
                        />
                      )}
                    </div>
                  </div>
                  {movePicker}
                  {extraPicker}
                </div>
              );
            }

            const hasGroupActions =
              !readOnly && (canOffer || showMove || showExtra || showUnassign);

            return (
              <div
                key={assignment.id}
                className={`group relative rounded-lg border p-3 ${
                  assignment.isLocked
                    ? "border-tennis-green bg-green-50/50"
                    : "border-gray-200"
                }`}
              >
                {/* Onzichtbare knop over de hele card opent het groep-detail;
                    het ⋮-menu vangt zijn eigen clicks af via pointer-events. */}
                {!moveOpen && !extraOpen && (
                  <button
                    type="button"
                    aria-label={groupName ?? ""}
                    onClick={() =>
                      assignment.groupId && onOpenGroup?.(assignment.groupId)
                    }
                    className="absolute inset-0 z-0 cursor-pointer rounded-lg transition-colors hover:bg-gray-50"
                  />
                )}
                <div className="pointer-events-none relative z-10">
                  {/* Header */}
                  <div className="mb-2 flex items-center justify-between gap-2">
                    <div className="flex min-w-0 flex-1 items-center gap-1.5">
                      <Users size={14} className="shrink-0 text-gray-400" />
                      <span className="truncate text-sm font-semibold text-gray-800">
                        {groupName}
                      </span>
                    </div>
                    <div className="flex shrink-0 items-center gap-1.5">
                      {statusBadge}
                      {assignment.isLocked &&
                        assignment.status === "Proposed" && (
                          <span className="inline-flex items-center gap-1 rounded bg-green-100 px-1.5 py-0.5 text-[10px] font-semibold text-green-700">
                            <Lock size={10} />
                            {t("locked")}
                          </span>
                        )}
                      {hasGroupActions && !moveOpen && !extraOpen && (
                        <span className="pointer-events-auto">
                          <AssignmentActionsMenu
                            name={groupName ?? ""}
                            isLocked={assignment.isLocked}
                            canOffer={canOffer}
                            showMove={showMove}
                            showExtra={showExtra}
                            showUnassign={showUnassign}
                            onLock={() =>
                              onLock(assignment.id, assignment.isLocked)
                            }
                            onOffer={() =>
                              setOfferTarget({
                                id: assignment.id,
                                name: groupName ?? "",
                              })
                            }
                            onMove={() =>
                              setMovingForAssignmentId(assignment.id)
                            }
                            onExtra={() =>
                              setAddingForAssignmentId(assignment.id)
                            }
                            onUnassign={() => onUnassign(assignment.id)}
                            lockPending={isLockPending}
                            offerPending={isOfferPending}
                            unassignPending={isUnassignPending}
                          />
                        </span>
                      )}
                      {!moveOpen && !extraOpen && (
                        <ChevronRight
                          size={16}
                          className="shrink-0 text-gray-300"
                        />
                      )}
                    </div>
                  </div>

                  {/* Alle leden */}
                  <div className="space-y-1 pl-1">
                    {people.map((person, ni) => {
                      const pColor = getAvatarColor(person.name);
                      return (
                        <div key={ni} className="flex items-center gap-2">
                          <div
                            className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[8px] font-bold ${pColor.bg} ${pColor.text}`}
                          >
                            {getInitials(person.name)}
                          </div>
                          <span className="truncate text-sm text-gray-700">
                            {person.name}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {movePicker}
                {extraPicker}
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

    <AlertDialog open={confirmDeleteOpen} onOpenChange={setConfirmDeleteOpen}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("deleteSlotConfirmTitle")}</AlertDialogTitle>
          <AlertDialogDescription>
            {t("deleteSlotConfirmBody")}
            {currentCount > 0 &&
              " " + t("deleteSlotConfirmBodyPeople", { count: currentCount })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t("cancel")}</AlertDialogCancel>
          <AlertDialogAction
            onClick={() => {
              setConfirmDeleteOpen(false);
              onDeleteSlot?.();
            }}
            className="bg-red-600 hover:bg-red-700"
          >
            {t("deleteSlotConfirmButton")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
    </>
  );
}
