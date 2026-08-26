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
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";
import { addWeeklyTemplateEntry } from "@/lib/api/lessonSeries";
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
export function AddWeekSlotDialog({
  seriesId,
  trainers,
  onClose,
  onSaved,
}: {
  seriesId: string;
  trainers: TrainerDto[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const [dayOfWeek, setDayOfWeek] = useState(0);
  const [trainerId, setTrainerId] = useState("");
  const [courtName, setCourtName] = useState("");
  const [startTime, setStartTime] = useState("18:00");
  const [endTime, setEndTime] = useState("19:00");
  const [maxStudents, setMaxStudents] = useState(4);
  const [saving, setSaving] = useState(false);

  const isValid = startTime !== "" && endTime < "24:00" && endTime > startTime;

  async function handleSave() {
    setSaving(true);
    try {
      await addWeeklyTemplateEntry(seriesId, {
        dayOfWeek,
        startTime,
        endTime,
        trainerId: trainerId || null,
        courtName: courtName.trim() || undefined,
        maxStudents,
      });
      toast.success("Weekslot toegevoegd");
      onSaved();
    } catch {
      // Error toast wordt al getoond door de axios interceptor
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Weekslot toevoegen</DialogTitle>
        </DialogHeader>
        <p className="text-xs text-gray-500 -mt-1">
          Dit lesmoment keert elke week terug op de gekozen dag, van vandaag tot
          het einde van de reeks. Losse lessen beheer je op de pagina Losse
          lessen.
        </p>

        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Dag van de week
            </label>
            <NativeSelect
              value={String(dayOfWeek)}
              onChange={(e) => setDayOfWeek(parseInt(e.target.value))}
              className="w-full"
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
            {saving ? "Bezig…" : "Toevoegen"}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
