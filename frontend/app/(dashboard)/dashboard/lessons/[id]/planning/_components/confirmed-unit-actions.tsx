"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { MoreVertical, ArrowRightLeft, Plus } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import type { ExtraSlotOption } from "./timeslot-detail-dialog";

const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

type SlotEntry = { assignmentId: string; label: string };
type Target = { enrollmentId?: string; groupId?: string };

type Step =
  | { kind: "menu" }
  | { kind: "chooseMove"; assignmentId: string }
  | { kind: "confirmMove"; assignmentId: string; slot: ExtraSlotOption }
  | { kind: "chooseExtra" };

/**
 * 3-puntjes-popover met acties voor een bevestigde persoon/groep. Handelt multi-slot
 * af: per ingepland slot een 'Verplaatsen'-actie, plus één 'Extra tijdslot'. De
 * doelslot-keuze en de 'lesnemer mailen'-bevestiging gebeuren in de popover zelf.
 */
export function ConfirmedUnitActions({
  unitName,
  slotEntries,
  target,
  options,
  onMove,
  onAssignExtra,
  isMovePending,
  isAssignPending,
}: {
  unitName: string;
  slotEntries: SlotEntry[];
  target: Target;
  options: ExtraSlotOption[];
  onMove: (assignmentId: string, slotId: string, notifyStudent: boolean) => void;
  onAssignExtra: (target: Target, slotId: string) => void;
  isMovePending: boolean;
  isAssignPending: boolean;
}) {
  const t = useTranslations("planning");
  const [open, setOpen] = useState(false);
  const [step, setStep] = useState<Step>({ kind: "menu" });
  const [notify, setNotify] = useState(true);

  function reset() {
    setStep({ kind: "menu" });
    setNotify(true);
  }

  const slotLabel = (s: ExtraSlotOption) =>
    `${DAY_NAMES_FULL[s.dayOfWeek]} ${s.startTime}–${s.endTime}`;

  return (
    <Popover
      open={open}
      onOpenChange={(o) => {
        setOpen(o);
        if (!o) reset();
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={t("actionsFor", { name: unitName })}
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700"
        >
          <MoreVertical size={16} />
        </button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-64 p-1.5 text-sm">
        {step.kind === "menu" && (
          <div className="space-y-0.5">
            <p className="px-2 pb-1 pt-1 text-[10px] font-semibold uppercase tracking-wide text-gray-400">
              {t("scheduledOn")}
            </p>
            {slotEntries.map((e) => (
              <div
                key={e.assignmentId}
                className="flex items-center justify-between gap-2 rounded-md px-2 py-1.5"
              >
                <span className="text-xs text-gray-700">{e.label}</span>
                <button
                  type="button"
                  onClick={() =>
                    setStep({ kind: "chooseMove", assignmentId: e.assignmentId })
                  }
                  className="inline-flex shrink-0 cursor-pointer items-center gap-1 rounded-md border border-tennis-green/40 px-2 py-1 text-[11px] font-semibold text-tennis-green transition-colors hover:bg-tennis-green/5"
                >
                  <ArrowRightLeft size={12} />
                  {t("moveAssignment")}
                </button>
              </div>
            ))}
            <div className="my-1 border-t border-gray-100" />
            <button
              type="button"
              onClick={() => setStep({ kind: "chooseExtra" })}
              className="flex w-full cursor-pointer items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs font-medium text-gray-600 transition-colors hover:bg-tennis-green/5 hover:text-tennis-green"
            >
              <Plus size={13} />
              {t("addExtraSlot")}
            </button>
          </div>
        )}

        {step.kind === "chooseMove" && (
          <SlotChooser
            title={t("chooseMoveSlot")}
            options={options}
            disabled={isMovePending}
            label={slotLabel}
            noneText={t("noOtherSlotAvailable")}
            cancelText={t("cancel")}
            onCancel={reset}
            onPick={(s) =>
              setStep({ kind: "confirmMove", assignmentId: step.assignmentId, slot: s })
            }
          />
        )}

        {step.kind === "confirmMove" && (
          <div className="space-y-2 p-1.5">
            <p className="text-xs leading-relaxed text-gray-600">
              {t("moveConfirmBody", {
                name: unitName,
                day: DAY_NAMES_FULL[step.slot.dayOfWeek],
                start: step.slot.startTime,
                end: step.slot.endTime,
              })}
            </p>
            <label className="flex cursor-pointer items-start gap-2 rounded-md border border-gray-200 bg-gray-50/50 px-2.5 py-2 text-xs text-gray-700">
              <input
                type="checkbox"
                checked={notify}
                onChange={(e) => setNotify(e.target.checked)}
                className="mt-0.5 h-3.5 w-3.5 shrink-0 accent-tennis-green"
              />
              <span>{t("moveNotifyLabel")}</span>
            </label>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={reset}
                className="cursor-pointer rounded-md px-2.5 py-1.5 text-xs text-gray-500 hover:text-gray-700"
              >
                {t("moveConfirmCancel")}
              </button>
              <button
                type="button"
                disabled={isMovePending}
                onClick={() => {
                  onMove(step.assignmentId, step.slot.id, notify);
                  setOpen(false);
                  reset();
                }}
                className="cursor-pointer rounded-md bg-tennis-green px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-tennis-green/90 disabled:opacity-50"
              >
                {t("moveConfirmButton")}
              </button>
            </div>
          </div>
        )}

        {step.kind === "chooseExtra" && (
          <SlotChooser
            title={t("chooseExtraSlot")}
            options={options}
            disabled={isAssignPending}
            label={slotLabel}
            noneText={t("noOtherSlotAvailable")}
            cancelText={t("cancel")}
            onCancel={reset}
            onPick={(s) => {
              onAssignExtra(target, s.id);
              setOpen(false);
              reset();
            }}
          />
        )}
      </PopoverContent>
    </Popover>
  );
}

function SlotChooser({
  title,
  options,
  disabled,
  label,
  noneText,
  cancelText,
  onPick,
  onCancel,
}: {
  title: string;
  options: ExtraSlotOption[];
  disabled: boolean;
  label: (s: ExtraSlotOption) => string;
  noneText: string;
  cancelText: string;
  onPick: (s: ExtraSlotOption) => void;
  onCancel: () => void;
}) {
  return (
    <div className="space-y-1.5 p-1.5">
      <p className="text-[11px] font-medium text-gray-500">{title}</p>
      {options.length === 0 ? (
        <p className="text-[11px] text-gray-400">{noneText}</p>
      ) : (
        <div className="max-h-56 space-y-1 overflow-auto">
          {options.map((s) => (
            <button
              key={s.id}
              type="button"
              disabled={disabled}
              onClick={() => onPick(s)}
              className="w-full cursor-pointer rounded-md border border-gray-200 px-2 py-1.5 text-left text-[11px] text-gray-700 transition-colors hover:border-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
            >
              <span className="font-medium">{label(s)}</span>
              {s.courtName && <span className="ml-1 text-gray-400">· {s.courtName}</span>}
            </button>
          ))}
        </div>
      )}
      <button
        type="button"
        onClick={onCancel}
        className="cursor-pointer text-[11px] text-gray-400 hover:text-gray-600"
      >
        {cancelText}
      </button>
    </div>
  );
}
