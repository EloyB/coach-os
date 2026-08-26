"use client";

import { useState } from "react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";
import { addWeeklyTemplateEntry, updateWeekSlot } from "@/lib/api/lessonSeries";
import { isAssignableTrainer, type TrainerDto } from "@/lib/api/trainers";

const WEEKDAY_NAMES = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

/**
 * Voegt een weekslot toe aan de weektemplate: het lesmoment keert elke week terug
 * en genereert lessen tot het einde van de reeks. Gebruikt in de planning-view.
 */
export interface WeekSlotEditData {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  trainerId?: string | null;
  courtName?: string | null;
  maxStudents: number;
  /** Aantal ingeplande studenten op dit slot; >0 vraagt een extra bevestiging bij aanpassen. */
  plannedCount?: number;
}

export function AddWeekSlotDialog({
  seriesId,
  trainers,
  editEntry,
  onClose,
  onSaved,
}: {
  seriesId: string;
  trainers: TrainerDto[];
  /** Aanwezig => aanpas-modus (bestaand weekslot); afwezig => toevoeg-modus. */
  editEntry?: WeekSlotEditData;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = editEntry != null;
  const [dayOfWeek, setDayOfWeek] = useState(editEntry?.dayOfWeek ?? 0);
  const [trainerId, setTrainerId] = useState(editEntry?.trainerId ?? "");
  const [courtName, setCourtName] = useState(editEntry?.courtName ?? "");
  const [startTime, setStartTime] = useState(editEntry?.startTime ?? "18:00");
  const [endTime, setEndTime] = useState(editEntry?.endTime ?? "19:00");
  const [maxStudents, setMaxStudents] = useState(editEntry?.maxStudents ?? 4);
  const [saving, setSaving] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  const plannedCount = editEntry?.plannedCount ?? 0;
  const isValid = startTime !== "" && endTime < "24:00" && endTime > startTime;

  async function doSubmit() {
    setSaving(true);
    try {
      if (isEdit) {
        await updateWeekSlot(seriesId, editEntry.id, {
          startTime,
          endTime,
          trainerId: trainerId || null,
          courtName: courtName.trim() || undefined,
          maxStudents,
        });
        toast.success("Weekslot aangepast");
      } else {
        await addWeeklyTemplateEntry(seriesId, {
          dayOfWeek,
          startTime,
          endTime,
          trainerId: trainerId || null,
          courtName: courtName.trim() || undefined,
          maxStudents,
        });
        toast.success("Weekslot toegevoegd");
      }
      onSaved();
    } catch {
      // Error toast wordt al getoond door de axios interceptor
    } finally {
      setSaving(false);
    }
  }

  function handleSave() {
    // Bij aanpassen van een slot waar al mensen op ingepland staan: eerst bevestigen.
    if (isEdit && plannedCount > 0) {
      setConfirmOpen(true);
      return;
    }
    void doSubmit();
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? "Weekslot aanpassen" : "Weekslot toevoegen"}
          </DialogTitle>
        </DialogHeader>
        <p className="text-xs text-gray-500 -mt-1">
          {isEdit
            ? "De wijziging geldt voor dit weekslot én al z'n lessen, zodat de planning meegaat. De dag ligt vast."
            : "Dit lesmoment keert elke week terug op de gekozen dag, van vandaag tot het einde van de reeks. Losse lessen beheer je op de pagina Losse lessen."}
        </p>

        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Dag van de week
            </label>
            <NativeSelect
              value={String(dayOfWeek)}
              onChange={(e) => setDayOfWeek(parseInt(e.target.value))}
              disabled={isEdit}
              className="w-full disabled:opacity-60"
            >
              {WEEKDAY_NAMES.map((name, index) => (
                <option key={index} value={index}>
                  {name}
                </option>
              ))}
            </NativeSelect>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                Starttijd
              </label>
              <input
                type="time"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                Eindtijd
              </label>
              <input
                type="time"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                className={inputClass}
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Trainer
            </label>
            <NativeSelect
              value={trainerId}
              onChange={(e) => setTrainerId(e.target.value)}
              className="w-full"
            >
              <option value="">Geen trainer</option>
              {trainers.filter(isAssignableTrainer).map((t) => (
                <option key={t.id} value={t.id}>
                  {t.firstName} {t.lastName}
                </option>
              ))}
            </NativeSelect>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Baan
            </label>
            <input
              type="text"
              value={courtName}
              onChange={(e) => setCourtName(e.target.value)}
              placeholder="Baan 1"
              className={inputClass}
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Max. leerlingen
            </label>
            <input
              type="number"
              min={1}
              value={maxStudents}
              onChange={(e) => setMaxStudents(parseInt(e.target.value) || 1)}
              className={inputClass}
            />
          </div>
        </div>

        {endTime <= startTime && (
          <p className="text-xs text-red-600">
            De eindtijd moet na de starttijd liggen.
          </p>
        )}

        <DialogFooter>
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-lg text-sm font-medium text-gray-600 hover:bg-gray-100 transition-colors"
          >
            Annuleren
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={saving || !isValid}
            className="px-4 py-2 rounded-lg bg-tennis-green text-white text-sm font-medium hover:bg-tennis-green/90 transition-colors disabled:opacity-50"
          >
            {saving ? "Bezig…" : isEdit ? "Opslaan" : "Toevoegen"}
          </button>
        </DialogFooter>

        <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Aanpassing bevestigen</AlertDialogTitle>
              <AlertDialogDescription>
                Er staan {plannedCount} inschrijving(en) ingepland op dit tijdsslot.
                De wijziging (tijd, trainer, baan, capaciteit) geldt voor het slot én
                al z&apos;n lessen. Wil je de aanpassing toch doorvoeren?
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <button
                type="button"
                onClick={() => setConfirmOpen(false)}
                className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
              >
                Annuleren
              </button>
              <button
                type="button"
                onClick={() => {
                  setConfirmOpen(false);
                  void doSubmit();
                }}
                className="rounded-lg bg-tennis-green px-4 py-2 text-sm font-semibold text-white hover:bg-tennis-green/90"
              >
                Ja, aanpassen
              </button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </DialogContent>
    </Dialog>
  );
}
