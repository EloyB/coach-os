"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  ChevronUp,
  Pencil,
  X,
  Trash2,
  Plus,
  Clock,
  CalendarDays,
  Euro,
  UserCheck,
  BarChart2,
  AlertTriangle,
  Building2,
  Users,
  Copy,
  CheckCircle2,
  ClipboardList,
  MoreVertical,
  Download,
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  getLessonSeriesById,
  updateLessonSeries,
  deleteLessonSeries,
  exportSeriePlanning,
  addWeeklyTemplateEntry,
  LessonDto,
} from "@/lib/api/lessonSeries";
import { downloadBlob } from "@/lib/download";
import {
  getLessonSeriesEnrollments,
  getEnrollmentForm,
  saveEnrollmentForm,
  cancelEnrollment,
  markEnrollmentCashPaid,
  updateBasicEnrollment,
} from "@/lib/api/enrollments";
import type {
  LessonSeriesEnrollmentDto,
  FormFieldDto,
  SaveFormFieldRequest,
} from "@/lib/api/enrollments";

import { getTennisClubs } from "@/lib/api/tennisClubs";
import { getMollieConnectionStatus } from "@/lib/api/mollieConnect";
import { getAuthUser } from "@/lib/auth";
import { FieldError } from "@/components/forms/field-error";
import { DatePicker } from "@/components/ui/date-picker";
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";
import { formatDateShort, formatDateNL } from "@/lib/date-utils";
import { enrollmentStatusStyles } from "@/lib/status-styles";
import { Badge } from "@/components/ui/badge";
import { PriceMatrixSection } from "./_components/price-matrix-section";

// ─── Edit Series Form ─────────────────────────────────────────────────────────

const editSchema = z
  .object({
    name: z.string().min(1, "Naam is verplicht").max(200),
    description: z.string().max(1000).optional(),
    tennisClubId: z.string().min(1, "Tennisclub is verplicht"),
    price: z.number().min(0),
    registrationDeadline: z.string().optional(),
    isActive: z.boolean(),
    minAge: z.number().int().min(0).max(120),
    maxAge: z.number().int().min(0).max(120),
    allowSoloEnrollment: z.boolean(),
    allowGroupEnrollment: z.boolean(),
    acceptOnlinePayment: z.boolean(),
    acceptManualPayment: z.boolean(),
  })
  .refine((d) => d.minAge <= d.maxAge, {
    message: "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd",
    path: ["maxAge"],
  })
  .refine((d) => d.allowSoloEnrollment || d.allowGroupEnrollment, {
    message: "Kies minstens één inschrijfwijze",
    path: ["allowGroupEnrollment"],
  })
  .refine((d) => d.acceptOnlinePayment || d.acceptManualPayment, {
    message: "Kies minstens één betaalmethode",
    path: ["acceptManualPayment"],
  });

type EditFormValues = z.infer<typeof editSchema>;

function EditSeriesForm({
  seriesId,
  defaultValues,
  onCancel,
  onSaved,
}: {
  seriesId: string;
  defaultValues: EditFormValues;
  onCancel: () => void;
  onSaved: () => void;
}) {
  const queryClient = useQueryClient();
  const { data: tennisClubs } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });
  const { data: mollie } = useQuery({
    queryKey: ["mollieStatus"],
    queryFn: getMollieConnectionStatus,
  });
  const mollieConnected = mollie?.connected ?? false;

  const mutation = useMutation({
    mutationFn: (data: EditFormValues) =>
      updateLessonSeries(seriesId, {
        tennisClubId: data.tennisClubId,
        name: data.name,
        description: data.description,
        price: data.price,
        registrationDeadline: data.registrationDeadline || undefined,
        isActive: data.isActive,
        minAge: data.minAge,
        maxAge: data.maxAge,
        allowSoloEnrollment: data.allowSoloEnrollment,
        allowGroupEnrollment: data.allowGroupEnrollment,
        acceptOnlinePayment: data.acceptOnlinePayment,
        acceptManualPayment: data.acceptManualPayment,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries"] });
      onSaved();
    },
  });

  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    defaultValues,
  });

  // Als de Mollie-koppeling niet (meer) actief is, kan online betalen nooit
  // aangevinkt staan — forceer dit zodra de status binnen is.
  useEffect(() => {
    if (mollie && !mollieConnected) {
      setValue("acceptOnlinePayment", false);
    }
  }, [mollie, mollieConnected, setValue]);

  return (
    <form
      onSubmit={handleSubmit((d) => mutation.mutate(d))}
      className="space-y-4 pt-4 border-t border-gray-100"
    >
      <div className="grid grid-cols-1 gap-4">
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">
            Naam
          </label>
          <input {...register("name")} className={inputClass} />
          <FieldError message={errors.name?.message} />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">
            Omschrijving
          </label>
          <textarea
            {...register("description")}
            rows={2}
            className={inputClass + " resize-none"}
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">
            Tennisclub
          </label>
          <Controller
            control={control}
            name="tennisClubId"
            render={({ field }) => (
              <Select onValueChange={field.onChange} value={field.value}>
                <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm">
                  <SelectValue placeholder="Kies tennisclub" />
                </SelectTrigger>
                <SelectContent>
                  {tennisClubs?.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          <FieldError message={errors.tennisClubId?.message} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Prijs (€)
            </label>
            <input
              {...register("price", { valueAsNumber: true })}
              type="number"
              min={0}
              step={0.01}
              className={inputClass}
            />
            <FieldError message={errors.price?.message} />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Inschrijfdeadline
            </label>
            <Controller
              control={control}
              name="registrationDeadline"
              render={({ field }) => (
                <DatePicker
                  value={field.value ?? ""}
                  onChange={field.onChange}
                  placeholder="Optioneel"
                />
              )}
            />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Min. leeftijd
            </label>
            <input
              {...register("minAge", { valueAsNumber: true })}
              type="number"
              min={0}
              max={120}
              className={inputClass}
            />
            <FieldError message={errors.minAge?.message} />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Max. leeftijd
            </label>
            <input
              {...register("maxAge", { valueAsNumber: true })}
              type="number"
              min={0}
              max={120}
              className={inputClass}
            />
            <FieldError message={errors.maxAge?.message} />
          </div>
        </div>
        <div className="flex items-center gap-2">
          <input
            {...register("isActive")}
            type="checkbox"
            id="isActive"
            className="w-4 h-4 accent-tennis-green"
          />
          <label htmlFor="isActive" className="text-sm text-gray-700">
            Actief
          </label>
        </div>

        {/* Inschrijfwijze */}
        <fieldset>
          <legend className="block text-xs font-medium text-gray-600 mb-1">
            Inschrijfwijze
          </legend>
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              {...register("allowSoloEnrollment")}
              className="w-4 h-4 accent-tennis-green"
            />
            Solo inschrijven
          </label>
          <label className="flex items-center gap-2 text-sm text-gray-700 mt-1">
            <input
              type="checkbox"
              {...register("allowGroupEnrollment")}
              className="w-4 h-4 accent-tennis-green"
            />
            In groep inschrijven
          </label>
          <FieldError message={errors.allowGroupEnrollment?.message} />
        </fieldset>

        {/* Betaalmethodes */}
        <fieldset>
          <legend className="block text-xs font-medium text-gray-600 mb-1">
            Betaalmethodes
          </legend>
          <label
            className={`flex items-center gap-2 text-sm text-gray-700 ${!mollieConnected ? "opacity-50" : ""}`}
          >
            <input
              type="checkbox"
              disabled={!mollieConnected}
              {...register("acceptOnlinePayment")}
              className="w-4 h-4 accent-tennis-green"
            />
            Online betalen (Mollie)
          </label>
          {!mollieConnected && (
            <p className="text-xs text-gray-500 mt-1">
              Online betalen kan pas nadat je met Mollie verbonden bent.{" "}
              <Link href="/dashboard/settings" className="underline">
                Verbind Mollie in instellingen
              </Link>
            </p>
          )}
          <label className="flex items-center gap-2 text-sm text-gray-700 mt-2">
            <input
              type="checkbox"
              {...register("acceptManualPayment")}
              className="w-4 h-4 accent-tennis-green"
            />
            Overschrijving
          </label>
          <FieldError message={errors.acceptManualPayment?.message} />
        </fieldset>
      </div>

      {mutation.isError && (
        <p className="text-xs text-red-500">
          Er ging iets mis. Probeer opnieuw.
        </p>
      )}

      <div className="flex items-center gap-2 pt-1">
        <button
          type="submit"
          disabled={mutation.isPending}
          className="flex items-center gap-1.5 px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
        >
          {mutation.isPending ? "Opslaan..." : "Opslaan"}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="flex items-center gap-1.5 px-4 py-2 bg-gray-100 text-gray-600 text-xs font-medium rounded-lg hover:bg-gray-200 transition-colors"
        >
          Annuleren
        </button>
      </div>
    </form>
  );
}

// ─── Lesson Week View (paginated calendar) ───────────────────────────────────

import {
  CalendarGrid,
  parseTime,
  getTrainerColor,
  type CalendarSlot,
} from "@/components/calendar/calendar-grid";
import { getTrainers, isAssignableTrainer } from "@/lib/api/trainers";
import type { TrainerDto } from "@/lib/api/trainers";

function toLocalISODate(d: Date): string {
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

interface WeekInfo {
  index: number;
  startDate: string;
  endDate: string;
  days: string[];
}

function computeWeeks(startDate: string, endDate: string): WeekInfo[] {
  const start = new Date(startDate + "T00:00:00");
  const end = new Date(endDate + "T00:00:00");

  const dow = start.getDay();
  const daysBack = dow === 0 ? 6 : dow - 1;
  const firstMonday = new Date(start);
  firstMonday.setDate(firstMonday.getDate() - daysBack);

  const weeks: WeekInfo[] = [];
  const current = new Date(firstMonday);
  let index = 0;

  while (current <= end) {
    const sunday = new Date(current);
    sunday.setDate(sunday.getDate() + 6);

    weeks.push({
      index,
      startDate: toLocalISODate(current),
      endDate: toLocalISODate(sunday),
      days: Array.from({ length: 7 }, (_, i) => {
        const d = new Date(current);
        d.setDate(d.getDate() + i);
        return toLocalISODate(d);
      }),
    });

    current.setDate(current.getDate() + 7);
    index++;
  }

  return weeks;
}

function dayOfWeekFromDate(dateStr: string, weekDays: string[]): number {
  const idx = weekDays.indexOf(dateStr);
  return idx >= 0 ? idx : new Date(dateStr + "T00:00:00").getDay();
}

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { updateLesson } from "@/lib/api/lessonSeries";
import { rescheduleLesson } from "@/lib/api/lessons";
import type { UpdateLessonRequest } from "@/lib/api/lessonSeries";

function EditLessonDialog({
  lesson,
  seriesId,
  trainers,
  onClose,
  onSaved,
}: {
  lesson: LessonDto;
  seriesId: string;
  trainers: TrainerDto[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const queryClient = useQueryClient();
  const [date, setDate] = useState(lesson.date);
  const [trainerId, setTrainerId] = useState(lesson.trainerId ?? "");
  const [courtName, setCourtName] = useState(lesson.courtName ?? "");
  const [startTime, setStartTime] = useState(lesson.startTime);
  const [endTime, setEndTime] = useState(lesson.endTime);
  const [maxStudents, setMaxStudents] = useState(lesson.maxStudents);
  const [notes, setNotes] = useState(lesson.notes ?? "");
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [showActionsMenu, setShowActionsMenu] = useState(false);
  const [activeAction, setActiveAction] = useState<"cancel" | "delete" | "reschedule" | null>(null);
  const [cancellationReason, setCancellationReason] = useState("");
  const [rescheduleDate, setRescheduleDate] = useState(lesson.date);
  const [rescheduleStart, setRescheduleStart] = useState(lesson.startTime);
  const [rescheduleEnd, setRescheduleEnd] = useState(lesson.endTime);
  const [rescheduleReason, setRescheduleReason] = useState("");
  const [rescheduling, setRescheduling] = useState(false);

  async function handleSave() {
    setSaving(true);
    try {
      const request: UpdateLessonRequest = {
        date,
        trainerId: trainerId || null,
        courtName,
        startTime,
        endTime,
        maxStudents,
        notes,
      };

      await updateLesson(seriesId, lesson.id, request);
      toast.success("Lesmoment opgeslagen");
      onSaved();
    } catch {
      // Error toast is already shown by axios interceptor
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    setDeleting(true);
    try {
      const { deleteLesson } = await import("@/lib/api/lessonSeries");
      await deleteLesson(seriesId, lesson.id);
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      onClose();
    } finally {
      setDeleting(false);
    }
  }

  async function handleCancel() {
    setCancelling(true);
    try {
      await updateLesson(seriesId, lesson.id, {
        isCancelled: true,
        cancellationReason: cancellationReason.trim() || undefined,
      });
      toast.success("Lesmoment geannuleerd");
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      onSaved();
    } catch {
      // Error toast shown by axios interceptor
    } finally {
      setCancelling(false);
    }
  }

  async function handleReschedule() {
    setRescheduling(true);
    try {
      const result = await rescheduleLesson(lesson.id, {
        newDate: rescheduleDate,
        newStartTime: rescheduleStart,
        newEndTime: rescheduleEnd,
        reason: rescheduleReason.trim() || undefined,
      });
      toast.success(
        `Lesmoment verplaatst — ${result.notifiedCount} student(en) genotifieerd`,
      );
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      onSaved();
    } catch {
      // Error toast shown by axios interceptor
    } finally {
      setRescheduling(false);
    }
  }

  async function handleUndoCancel() {
    setCancelling(true);
    try {
      await updateLesson(seriesId, lesson.id, { isCancelled: false });
      toast.success("Annulatie ongedaan gemaakt");
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      onSaved();
    } catch {
      // Error toast shown by axios interceptor
    } finally {
      setCancelling(false);
    }
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Lesmoment bewerken</DialogTitle>
        </DialogHeader>

        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Datum
            </label>
            <DatePicker value={date} onChange={setDate} />
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
              {trainers
                .filter((t) => isAssignableTrainer(t) || t.id === trainerId)
                .map((t) => (
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

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Notities
            </label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
              className={inputClass + " resize-none"}
            />
          </div>
        </div>

        {/* Geannuleerd banner */}
        {lesson.isCancelled && (
          <div className="flex items-center gap-2 px-3 py-2 bg-orange-50 border border-orange-200 rounded-lg text-xs text-orange-700">
            <AlertTriangle size={12} className="shrink-0" />
            <span className="flex-1">
              Geannuleerd{lesson.cancellationReason && ` — ${lesson.cancellationReason}`}
            </span>
            <button
              type="button"
              onClick={handleUndoCancel}
              disabled={cancelling}
              className="font-medium underline underline-offset-2 hover:no-underline disabled:opacity-60"
            >
              Ongedaan maken
            </button>
          </div>
        )}

        {/* Actie bevestiging inline */}
        {activeAction === "cancel" && (
          <div className="border border-orange-200 rounded-lg p-3 space-y-2 bg-orange-50/50">
            <p className="text-xs font-medium text-orange-800">Les annuleren</p>
            <textarea
              value={cancellationReason}
              onChange={(e) => setCancellationReason(e.target.value)}
              placeholder="Reden (optioneel) — wordt meegezonden in de mail aan studenten"
              rows={2}
              className={inputClass + " resize-none text-xs"}
            />
            <div className="flex gap-2">
              <button
                type="button"
                onClick={handleCancel}
                disabled={cancelling}
                className="px-3 py-1.5 bg-orange-600 text-white text-xs font-semibold rounded-lg hover:bg-orange-700 disabled:opacity-60"
              >
                {cancelling ? "Bezig..." : "Annuleer les"}
              </button>
              <button
                type="button"
                onClick={() => { setActiveAction(null); setCancellationReason(""); }}
                className="px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700"
              >
                Terug
              </button>
            </div>
          </div>
        )}

        {activeAction === "reschedule" && (
          <div className="border border-tennis-green/30 rounded-lg p-3 space-y-2 bg-tennis-green/5">
            <p className="text-xs font-medium text-tennis-green">Lesmoment verplaatsen</p>
            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="block text-[11px] font-medium text-gray-600 mb-1">
                  Nieuwe datum
                </label>
                <DatePicker value={rescheduleDate} onChange={setRescheduleDate} />
              </div>
              <div className="grid grid-cols-2 gap-1">
                <div>
                  <label className="block text-[11px] font-medium text-gray-600 mb-1">
                    Start
                  </label>
                  <input
                    type="time"
                    value={rescheduleStart}
                    onChange={(e) => setRescheduleStart(e.target.value)}
                    className={inputClass + " text-xs"}
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-medium text-gray-600 mb-1">
                    Eind
                  </label>
                  <input
                    type="time"
                    value={rescheduleEnd}
                    onChange={(e) => setRescheduleEnd(e.target.value)}
                    className={inputClass + " text-xs"}
                  />
                </div>
              </div>
            </div>
            <textarea
              value={rescheduleReason}
              onChange={(e) => setRescheduleReason(e.target.value)}
              placeholder="Reden (optioneel) — wordt meegezonden in de mail aan studenten"
              rows={2}
              className={inputClass + " resize-none text-xs"}
            />
            <div className="flex gap-2">
              <button
                type="button"
                onClick={handleReschedule}
                disabled={rescheduling}
                className="px-3 py-1.5 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 disabled:opacity-60"
              >
                {rescheduling ? "Bezig..." : "Verplaats les"}
              </button>
              <button
                type="button"
                onClick={() => { setActiveAction(null); setRescheduleReason(""); }}
                className="px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700"
              >
                Terug
              </button>
            </div>
          </div>
        )}

        {activeAction === "delete" && (
          <div className="border border-red-200 rounded-lg p-3 bg-red-50/50">
            <p className="text-xs font-medium text-red-800 mb-2">Lesmoment permanent verwijderen?</p>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={handleDelete}
                disabled={deleting}
                className="px-3 py-1.5 bg-red-600 text-white text-xs font-semibold rounded-lg hover:bg-red-700 disabled:opacity-60"
              >
                {deleting ? "Bezig..." : "Verwijder"}
              </button>
              <button
                type="button"
                onClick={() => setActiveAction(null)}
                className="px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700"
              >
                Terug
              </button>
            </div>
          </div>
        )}

        {/* Footer: opslaan + ⋮ menu */}
        {!activeAction && (
          <div className="flex items-center gap-2 pt-2">
            <button
              type="button"
              onClick={handleSave}
              disabled={saving}
              className="flex-1 px-4 py-2 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 disabled:opacity-60"
            >
              {saving ? "Opslaan..." : "Opslaan"}
            </button>
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border border-gray-200 text-sm font-medium text-gray-600 rounded-lg hover:bg-gray-50"
            >
              Sluiten
            </button>

            {/* ⋮ acties menu */}
            <div className="relative">
              <button
                type="button"
                onClick={() => setShowActionsMenu((v) => !v)}
                className="w-9 h-9 flex items-center justify-center border border-gray-200 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-50 transition-colors"
              >
                <MoreVertical size={15} />
              </button>
              {showActionsMenu && (
                <div className="absolute bottom-full right-0 mb-1 bg-white border border-gray-100 rounded-lg shadow-lg z-50 min-w-44 py-1 text-sm">
                  {!lesson.isCancelled && (
                    <>
                      <button
                        type="button"
                        onClick={() => { setShowActionsMenu(false); setActiveAction("reschedule"); }}
                        className="w-full flex items-center gap-2 px-3 py-2 text-tennis-green hover:bg-tennis-green/5 text-left"
                      >
                        <CalendarDays size={13} />
                        Verplaatsen
                      </button>
                      <button
                        type="button"
                        onClick={() => { setShowActionsMenu(false); setActiveAction("cancel"); }}
                        className="w-full flex items-center gap-2 px-3 py-2 text-orange-600 hover:bg-orange-50 text-left"
                      >
                        <AlertTriangle size={13} />
                        Les annuleren
                      </button>
                    </>
                  )}
                  <button
                    type="button"
                    onClick={() => { setShowActionsMenu(false); setActiveAction("delete"); }}
                    className="w-full flex items-center gap-2 px-3 py-2 text-red-600 hover:bg-red-50 text-left"
                  >
                    <Trash2 size={13} />
                    Verwijderen
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

      </DialogContent>
    </Dialog>
  );
}

const WEEKDAY_NAMES = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

function AddWeekSlotDialog({
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

function LessonWeekView({
  lessons,
  startDate,
  endDate,
  trainers,
  seriesId,
}: {
  lessons: LessonDto[];
  startDate: string;
  endDate: string;
  trainers: TrainerDto[];
  seriesId: string;
}) {
  const weeks = computeWeeks(startDate, endDate);
  const initialWeekIndex = (() => {
    if (lessons.length === 0) return 0;
    const firstLessonDate = lessons
      .map((l) => l.date)
      .sort()[0];
    const idx = weeks.findIndex(
      (w) => firstLessonDate >= w.startDate && firstLessonDate <= w.endDate,
    );
    return idx === -1 ? 0 : idx;
  })();
  const [weekIndex, setWeekIndex] = useState(initialWeekIndex);
  const [editingLesson, setEditingLesson] = useState<LessonDto | null>(null);
  const [addingLesson, setAddingLesson] = useState(false);
  const queryClient = useQueryClient();
  const currentWeek = weeks[weekIndex];

  if (!currentWeek) return null;

  // Build trainer name lookup
  const trainerMap = new Map(
    trainers.map((t) => [t.id, `${t.firstName} ${t.lastName}`]),
  );

  // Convert lessons for the current week into CalendarSlots
  const weekSlots: CalendarSlot[] = lessons
    .filter(
      (l) => l.date >= currentWeek.startDate && l.date <= currentWeek.endDate,
    )
    .map((l) => ({
      id: l.id,
      dayOfWeek: dayOfWeekFromDate(l.date, currentWeek.days),
      startTime: l.startTime,
      endTime: l.endTime,
      trainerId: l.trainerId ?? null,
      trainerName: l.trainerId ? (trainerMap.get(l.trainerId) ?? null) : null,
      courtName: l.courtName,
      maxStudents: l.maxStudents,
      isCancelled: l.isCancelled,
    }));

  // Collect unique trainers used in lessons for the legend
  const usedTrainerIds = new Set(
    lessons.filter((l) => l.trainerId).map((l) => l.trainerId!),
  );
  const legendTrainers = trainers.filter((t) => usedTrainerIds.has(t.id));

  const dayDates = currentWeek.days.map(formatDateShort);

  // Compute dynamic hour range from all lessons
  let calStartHour: number | undefined;
  let calEndHour: number | undefined;
  if (lessons.length > 0) {
    let minMin = Infinity;
    let maxMin = -Infinity;
    for (const l of lessons) {
      const start = parseTime(l.startTime);
      const end = parseTime(l.endTime);
      if (start < minMin) minMin = start;
      if (end > maxMin) maxMin = end;
    }
    calStartHour = Math.max(0, Math.floor(minMin / 60) - 1) + 0.5;
    calEndHour = Math.min(24, Math.ceil(maxMin / 60) + 1);
  }

  return (
    <div>
      {/* Header with pagination */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2.5">
          <h2 className="text-sm font-semibold text-gray-800">Lesmomenten</h2>
          <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-tennis-green/10 text-tennis-green text-xs font-bold">
            {lessons.length}
          </span>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setAddingLesson(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 mr-1 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors"
          >
            <Plus size={12} />
            Weekslot toevoegen
          </button>
          <span className="text-xs text-gray-500">
            Week {weekIndex + 1} van {weeks.length}
          </span>
          <span className="text-xs text-gray-400">
            {formatDateShort(currentWeek.startDate)} –{" "}
            {formatDateShort(currentWeek.endDate)}
          </span>
          <div className="flex items-center gap-1 ml-1">
            <button
              type="button"
              onClick={() => setWeekIndex((i) => Math.max(0, i - 1))}
              disabled={weekIndex === 0}
              className="w-7 h-7 flex items-center justify-center rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronLeft size={14} />
            </button>
            <button
              type="button"
              onClick={() =>
                setWeekIndex((i) => Math.min(weeks.length - 1, i + 1))
              }
              disabled={weekIndex === weeks.length - 1}
              className="w-7 h-7 flex items-center justify-center rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      </div>

      {/* Calendar grid — click slot to edit */}
      <CalendarGrid
        slots={weekSlots}
        readOnly
        dayDates={dayDates}
        startHour={calStartHour}
        endHour={calEndHour}
        onSlotClick={(slot) => {
          const lesson = lessons.find((l) => l.id === slot.id);
          if (lesson) setEditingLesson(lesson);
        }}
      />

      {/* Add week-slot dialog */}
      {addingLesson && (
        <AddWeekSlotDialog
          seriesId={seriesId}
          trainers={trainers}
          onClose={() => setAddingLesson(false)}
          onSaved={() => {
            setAddingLesson(false);
            queryClient.invalidateQueries({
              queryKey: ["lessonSeries", seriesId],
            });
          }}
        />
      )}

      {/* Edit lesson dialog */}
      {editingLesson && (
        <EditLessonDialog
          lesson={editingLesson}
          seriesId={seriesId}
          trainers={trainers}
          onClose={() => setEditingLesson(null)}
          onSaved={() => {
            setEditingLesson(null);
            queryClient.invalidateQueries({
              queryKey: ["lessonSeries", seriesId],
            });
          }}
        />
      )}

      {/* Trainer legend */}
      {legendTrainers.length > 0 && (
        <div className="flex items-center gap-4 mt-3 px-1 flex-wrap">
          {usedTrainerIds.has(undefined as unknown as string) ||
          lessons.some((l) => !l.trainerId) ? (
            <div className="flex items-center gap-1.5 text-xs text-gray-500">
              <div
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: getTrainerColor(null).border }}
              />
              Geen trainer
            </div>
          ) : null}
          {legendTrainers.map((t) => {
            const color = getTrainerColor(t.id);
            return (
              <div
                key={t.id}
                className="flex items-center gap-1.5 text-xs text-gray-500"
              >
                <div
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: color.border }}
                />
                {t.firstName} {t.lastName}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ─── Form Builder Section ─────────────────────────────────────────────────────

const FIELD_TYPES = [
  { value: 1, label: "Vrije tekst" },
  { value: 2, label: "Meerkeuze" },
  { value: 3, label: "Ja/Nee" },
];

interface DraftField extends SaveFormFieldRequest {
  _key: string;
}

function FormBuilderSection({ seriesId }: { seriesId: string }) {
  const queryClient = useQueryClient();
  const [fields, setFields] = useState<DraftField[]>([]);
  const [loaded, setLoaded] = useState(false);

  const { data: existingForm } = useQuery({
    queryKey: ["enrollmentForm", seriesId],
    queryFn: () => getEnrollmentForm(seriesId),
  });

  // Initialise draft fields from loaded form
  useEffect(() => {
    if (!loaded && existingForm !== undefined) {
      if (existingForm) {
        setFields(
          existingForm.fields.map((f) => ({
            _key: f.id,
            id: f.id,
            label: f.label,
            type: f.type,
            isRequired: f.isRequired,
            order: f.order,
            options: f.options ?? undefined,
          })),
        );
      }
      setLoaded(true);
    }
  }, [existingForm, loaded]);

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload: SaveFormFieldRequest[] = fields.map((f, i) => ({
        id: f.id,
        label: f.label,
        type: f.type,
        isRequired: f.isRequired,
        order: i,
        options: f.type === 2 ? f.options : undefined,
      }));
      return saveEnrollmentForm(seriesId, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["enrollmentForm", seriesId] });
    },
  });

  function addField() {
    setFields((prev) => [
      ...prev,
      {
        _key: Math.random().toString(36).slice(2),
        label: "",
        type: 1,
        isRequired: false,
        order: prev.length,
      },
    ]);
  }

  function updateField(key: string, updates: Partial<DraftField>) {
    setFields((prev) =>
      prev.map((f) => (f._key === key ? { ...f, ...updates } : f)),
    );
  }

  function removeField(key: string) {
    setFields((prev) => prev.filter((f) => f._key !== key));
  }

  function moveField(key: string, direction: -1 | 1) {
    setFields((prev) => {
      const idx = prev.findIndex((f) => f._key === key);
      if (idx < 0) return prev;
      const newIdx = idx + direction;
      if (newIdx < 0 || newIdx >= prev.length) return prev;
      const next = [...prev];
      [next[idx], next[newIdx]] = [next[newIdx], next[idx]];
      return next;
    });
  }

  function addOption(key: string) {
    setFields((prev) =>
      prev.map((f) =>
        f._key === key ? { ...f, options: [...(f.options ?? []), ""] } : f,
      ),
    );
  }

  function updateOption(key: string, optIdx: number, value: string) {
    setFields((prev) =>
      prev.map((f) => {
        if (f._key !== key) return f;
        const opts = [...(f.options ?? [])];
        opts[optIdx] = value;
        return { ...f, options: opts };
      }),
    );
  }

  function removeOption(key: string, optIdx: number) {
    setFields((prev) =>
      prev.map((f) => {
        if (f._key !== key) return f;
        const opts = (f.options ?? []).filter((_, i) => i !== optIdx);
        return { ...f, options: opts };
      }),
    );
  }

  const inputCls =
    "w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:border-tennis-green bg-white";

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
          <ClipboardList size={13} className="text-tennis-green" />
        </div>
        <h2 className="text-sm font-semibold text-gray-800">
          Inschrijfformulier
        </h2>
      </div>

      <div className="p-5 space-y-4">
        {/* Predefined fields badge list */}
        <div>
          <p className="text-xs text-gray-400 mb-2">
            Vaste velden (altijd zichtbaar)
          </p>
          <div className="flex flex-wrap gap-2">
            {[
              "Voornaam",
              "Achternaam",
              "E-mailadres",
              "Inschrijvingstype",
              "Beschikbaarheid",
            ].map((f) => (
              <span
                key={f}
                className="inline-flex items-center px-2.5 py-1 rounded-full text-xs bg-tennis-green/10 text-tennis-green font-medium"
              >
                {f}
              </span>
            ))}
          </div>
        </div>

        {/* Custom fields */}
        {fields.length === 0 && (
          <p className="text-xs text-gray-400 py-2">
            Nog geen aangepaste velden. Klik &apos;Veld toevoegen&apos; om te
            beginnen.
          </p>
        )}

        <div className="space-y-3">
          {fields.map((field, idx) => (
            <div
              key={field._key}
              className="border border-gray-100 rounded-xl p-4 space-y-3 bg-[#FAFAF8]"
            >
              {/* Row 1: label (full width) */}
              <input
                type="text"
                placeholder="Veldlabel, bijv. 'Telefoonnummer'"
                value={field.label}
                onChange={(e) =>
                  updateField(field._key, { label: e.target.value })
                }
                className={inputCls}
              />

              {/* Row 2: type + required + reorder + delete */}
              <div className="flex items-center gap-2">
                <NativeSelect
                  value={field.type}
                  onChange={(e) =>
                    updateField(field._key, {
                      type: parseInt(e.target.value),
                      options: undefined,
                    })
                  }
                >
                  {FIELD_TYPES.map((t) => (
                    <option key={t.value} value={t.value}>
                      {t.label}
                    </option>
                  ))}
                </NativeSelect>

                <label className="flex items-center gap-1.5 text-xs text-gray-600 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    checked={field.isRequired}
                    onChange={(e) =>
                      updateField(field._key, { isRequired: e.target.checked })
                    }
                    className="accent-tennis-green"
                  />
                  Verplicht
                </label>

                <div className="flex items-center gap-1 ml-auto">
                  <button
                    type="button"
                    onClick={() => moveField(field._key, -1)}
                    disabled={idx === 0}
                    className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 disabled:opacity-30 transition-colors"
                  >
                    <ChevronUp size={13} />
                  </button>
                  <button
                    type="button"
                    onClick={() => moveField(field._key, 1)}
                    disabled={idx === fields.length - 1}
                    className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 disabled:opacity-30 transition-colors"
                  >
                    <ChevronDown size={13} />
                  </button>
                  <button
                    type="button"
                    onClick={() => removeField(field._key)}
                    className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors"
                  >
                    <Trash2 size={13} />
                  </button>
                </div>
              </div>

              {/* Options for MultipleChoice */}
              {field.type === 2 && (
                <div className="space-y-2 pt-1 border-t border-gray-100">
                  {(field.options ?? []).map((opt, optIdx) => (
                    <div key={optIdx} className="flex items-center gap-2">
                      <input
                        type="text"
                        placeholder={`Optie ${optIdx + 1}`}
                        value={opt}
                        onChange={(e) =>
                          updateOption(field._key, optIdx, e.target.value)
                        }
                        className={inputCls + " flex-1"}
                      />
                      <button
                        type="button"
                        onClick={() => removeOption(field._key, optIdx)}
                        className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors"
                      >
                        <X size={13} />
                      </button>
                    </div>
                  ))}
                  <button
                    type="button"
                    onClick={() => addOption(field._key)}
                    className="flex items-center gap-1 text-xs text-tennis-green hover:underline"
                  >
                    <Plus size={11} />
                    Optie toevoegen
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>

        <div className="flex items-center gap-3 pt-1">
          <button
            type="button"
            onClick={addField}
            className="flex items-center gap-1.5 px-3 py-2 border border-gray-200 text-xs font-medium text-gray-600 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <Plus size={12} />
            Veld toevoegen
          </button>
          <button
            type="button"
            onClick={() => saveMutation.mutate()}
            disabled={saveMutation.isPending}
            className="flex items-center gap-1.5 px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
          >
            {saveMutation.isPending
              ? "Opslaan..."
              : saveMutation.isSuccess
                ? "Opgeslagen ✓"
                : "Formulier opslaan"}
          </button>
        </div>

        {saveMutation.isError && (
          <p className="text-xs text-red-500">
            Opslaan mislukt. Probeer opnieuw.
          </p>
        )}
      </div>
    </div>
  );
}

// ─── Enrollments Section ──────────────────────────────────────────────────────

const basicEnrollmentSchema = z.object({
  studentName: z.string().min(1, "Naam is verplicht").max(200),
  contactEmail: z.string().min(1, "Contact e-mailadres is verplicht").email("Ongeldig e-mailadres"),
  studentEmail: z
    .string()
    .email("Ongeldig e-mailadres")
    .optional()
    .or(z.literal("")),
  studentPhone: z.string().max(50).optional(),
  dateOfBirth: z.string().min(1, "Geboortedatum is verplicht"),
  isOpenToGrouping: z.boolean(),
});

type BasicEnrollmentFormValues = z.infer<typeof basicEnrollmentSchema>;

function EditEnrollmentDialog({
  enrollment,
  seriesId,
  open,
  onOpenChange,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const queryClient = useQueryClient();
  const form = useForm<BasicEnrollmentFormValues>({
    resolver: zodResolver(basicEnrollmentSchema),
    values: {
      studentName: enrollment.studentName,
      contactEmail: enrollment.contactEmail,
      studentEmail: enrollment.studentEmail ?? "",
      studentPhone: enrollment.studentPhone ?? "",
      dateOfBirth: enrollment.dateOfBirth ?? "",
      isOpenToGrouping: enrollment.isOpenToGrouping,
    },
  });

  const mutation = useMutation({
    mutationFn: (values: BasicEnrollmentFormValues) =>
      updateBasicEnrollment(seriesId, enrollment.id, {
        studentName: values.studentName,
        contactEmail: values.contactEmail,
        studentEmail: values.studentEmail?.trim() ? values.studentEmail : null,
        studentPhone: values.studentPhone?.trim() ? values.studentPhone : null,
        dateOfBirth: values.dateOfBirth,
        isOpenToGrouping: values.isOpenToGrouping,
      }),
    onSuccess: () => {
      toast.success("Inschrijving bijgewerkt");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["planning", seriesId] });
      onOpenChange(false);
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onClick={(e) => e.stopPropagation()} className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Inschrijving aanpassen</DialogTitle>
          <DialogDescription>
            Wijzig enkel basisgegevens. Betaling en planning blijven ongewijzigd.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={form.handleSubmit((values) => mutation.mutate(values))} className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label className="block text-xs font-medium text-gray-600 mb-1">Naam deelnemer</label>
              <input className={inputClass} {...form.register("studentName")} />
              <FieldError message={form.formState.errors.studentName?.message} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Contact e-mail</label>
              <input type="email" className={inputClass} {...form.register("contactEmail")} />
              <FieldError message={form.formState.errors.contactEmail?.message} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Eigen e-mail deelnemer</label>
              <input type="email" className={inputClass} {...form.register("studentEmail")} />
              <FieldError message={form.formState.errors.studentEmail?.message} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Telefoon</label>
              <input className={inputClass} {...form.register("studentPhone")} />
              <FieldError message={form.formState.errors.studentPhone?.message} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Geboortedatum</label>
              <input type="date" className={inputClass} {...form.register("dateOfBirth")} />
              <FieldError message={form.formState.errors.dateOfBirth?.message} />
            </div>
          </div>
          <label className="flex items-center gap-2 text-xs text-gray-600">
            <input type="checkbox" {...form.register("isOpenToGrouping")} />
            Open voor groepering met andere deelnemers
          </label>
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
            Deze wijziging past geen betaalstatus, betalingsbedrag of planningstoewijzing aan.
          </p>
          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className="px-3 py-2 rounded-lg border border-gray-200 text-sm font-medium text-gray-600 hover:bg-gray-50"
            >
              Annuleren
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="px-3 py-2 rounded-lg bg-tennis-green text-sm font-medium text-white hover:bg-tennis-green/90 disabled:opacity-50"
            >
              {mutation.isPending ? "Opslaan…" : "Opslaan"}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function EnrollmentRow({
  enrollment,
  seriesId,
  isPossibleDuplicate,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  isPossibleDuplicate: boolean;
}) {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [markingPaid, setMarkingPaid] = useState(false);
  const [editing, setEditing] = useState(false);
  const hasResponses = enrollment.formResponses.length > 0;
  const isCancelled = enrollment.status === "Cancelled";
  const isPendingPayment = enrollment.status === "PendingPayment";
  // De cash-betaling hangt aan de groepsleider (of aan een solo-inschrijving).
  // Alleen die rij mag "Markeer als betaald" tonen; groepsleden hebben geen
  // eigen betaling en zouden een 404 uitlokken.
  const ownsPayment =
    enrollment.enrollmentGroupId == null || enrollment.isGroupLeader;

  async function handleCancelEnrollment() {
    setCancelling(true);
    try {
      await cancelEnrollment(seriesId, enrollment.id);
      toast.success("Inschrijving geannuleerd");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
    } catch {
      // Error toast wordt al getoond door de axios interceptor
    } finally {
      setCancelling(false);
    }
  }

  async function handleMarkCashPaid(e: React.MouseEvent) {
    e.stopPropagation();
    setMarkingPaid(true);
    try {
      await markEnrollmentCashPaid(enrollment.id);
      toast.success("Inschrijving gemarkeerd als betaald");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
    } catch {
      // Error toast wordt al getoond door de axios interceptor
    } finally {
      setMarkingPaid(false);
    }
  }

  return (
    <div className="border-b border-gray-50 last:border-b-0">
      <div
        className={`flex items-center justify-between px-5 py-3 ${hasResponses ? "cursor-pointer hover:bg-gray-50/50" : ""} ${isCancelled ? "opacity-50" : ""}`}
        onClick={() => hasResponses && setExpanded((v) => !v)}
      >
        <div className="flex-1 min-w-0">
          <p
            className={`text-sm font-medium text-gray-800 truncate ${isCancelled ? "line-through" : ""}`}
          >
            {enrollment.studentName}
          </p>
          <p className="text-xs text-gray-400 truncate">
            {enrollment.hasOwnEmail
              ? enrollment.studentEmail
              : `via ${enrollment.contactEmail}`}
            {enrollment.studentPhone ? ` · ${enrollment.studentPhone}` : ""}
          </p>
        </div>
        <div className="flex items-center gap-3 ml-4">
          {isPossibleDuplicate && (
            <Badge className="border-0 text-xs bg-amber-100 text-amber-700">
              mogelijk dubbel
            </Badge>
          )}
          {enrollment.categoryLabel && (
            <Badge
              className={`border-0 text-xs ${
                enrollment.category === 2
                  ? "bg-sky-100 text-sky-700"
                  : "bg-gray-100 text-gray-600"
              }`}
            >
              {enrollment.categoryLabel}
            </Badge>
          )}
          <span className="text-xs text-gray-400">
            {new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
          </span>
          {enrollmentStatusStyles[enrollment.status] && (
            <Badge
              className={`${enrollmentStatusStyles[enrollment.status].className} border-0 text-xs`}
            >
              {enrollmentStatusStyles[enrollment.status].label}
            </Badge>
          )}
          {isPendingPayment && ownsPayment && (
            <button
              onClick={handleMarkCashPaid}
              disabled={markingPaid}
              aria-label="Markeer als betaald"
              className="flex items-center gap-1 px-2 py-1 rounded-md border border-tennis-green/20 text-xs font-medium text-tennis-green hover:bg-tennis-green/5 transition-colors disabled:opacity-50"
            >
              <Euro size={12} />
              Markeer als betaald
            </button>
          )}
          {!isCancelled && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                setEditing(true);
              }}
              aria-label={`Inschrijving van ${enrollment.studentName} aanpassen`}
              className="p-1 rounded-md text-gray-300 hover:text-tennis-green hover:bg-tennis-green/5 transition-colors"
            >
              <Pencil size={13} />
            </button>
          )}
          {!isCancelled && (
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <button
                  onClick={(e) => e.stopPropagation()}
                  disabled={cancelling}
                  aria-label={`Inschrijving van ${enrollment.studentName} annuleren`}
                  className="p-1 rounded-md text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors disabled:opacity-50"
                >
                  <Trash2 size={13} />
                </button>
              </AlertDialogTrigger>
              <AlertDialogContent onClick={(e) => e.stopPropagation()}>
                <AlertDialogHeader>
                  <AlertDialogTitle>Inschrijving annuleren?</AlertDialogTitle>
                  <AlertDialogDescription>
                    De inschrijving van {enrollment.studentName} wordt op
                    geannuleerd gezet en de plaats komt weer vrij. De
                    formulierantwoorden blijven bewaard.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Terug</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={handleCancelEnrollment}
                    className="bg-red-600 hover:bg-red-700"
                  >
                    Annuleren
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}
          {hasResponses &&
            (expanded ? (
              <ChevronUp size={13} className="text-gray-400" />
            ) : (
              <ChevronDown size={13} className="text-gray-400" />
            ))}
        </div>
      </div>

      {expanded && hasResponses && (
        <div className="px-5 pb-3">
          <dl className="space-y-1.5 bg-[#FAFAF8] rounded-lg p-3">
            {enrollment.formResponses.map((r, i) => (
              <div key={i} className="flex gap-3 text-xs">
                <dt className="text-gray-500 shrink-0 min-w-[120px]">
                  {r.fieldLabel}
                </dt>
                <dd className="text-gray-800 font-medium">{r.value}</dd>
              </div>
            ))}
          </dl>
        </div>
      )}

      <EditEnrollmentDialog
        enrollment={enrollment}
        seriesId={seriesId}
        open={editing}
        onOpenChange={setEditing}
      />
    </div>
  );
}

function EnrollmentsSection({ seriesId }: { seriesId: string }) {
  const [copied, setCopied] = useState(false);

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ["enrollments", seriesId],
    queryFn: () => getLessonSeriesEnrollments(seriesId),
  });

  function handleCopyLink() {
    const url = `${window.location.origin}/enroll/${seriesId}`;
    navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  const active = enrollments.filter(
    (e) => e.status === "Confirmed" || e.status === "Pending",
  );

  // Zelfde persoon (contactadres + genormaliseerde naam) meer dan één keer =
  // mogelijk een dubbele inschrijving. Client-side afgeleid uit de lijst die er al is.
  const duplicateIds = new Set<string>(
    (() => {
      const keyed = enrollments.map((e) => ({
        id: e.id,
        key: `${e.contactEmail}|${e.studentName.trim().toLowerCase()}`,
      }));
      return keyed
        .filter(
          (row) => keyed.filter((other) => other.key === row.key).length > 1,
        )
        .map((row) => row.id);
    })(),
  );

  return (
    <div
      id="enrollments"
      className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden scroll-mt-20"
    >
      <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <h2 className="text-sm font-semibold text-gray-800">
            Inschrijvingen
          </h2>
          <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-tennis-green/10 text-tennis-green text-xs font-bold">
            {active.length}
          </span>
        </div>
        <button
          onClick={handleCopyLink}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors"
        >
          {copied ? (
            <>
              <CheckCircle2 size={12} className="text-green-500" />
              Link gekopieerd
            </>
          ) : (
            <>
              <Copy size={12} />
              Inschrijflink
            </>
          )}
        </button>
      </div>

      {isLoading ? (
        <div className="p-8 text-center">
          <div className="w-4 h-4 border-2 border-tennis-green/30 border-t-tennis-green rounded-full animate-spin mx-auto" />
        </div>
      ) : enrollments.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-10 text-center">
          <div className="w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center mb-2">
            <Users size={15} className="text-gray-400" />
          </div>
          <p className="text-sm text-gray-400">Nog geen inschrijvingen</p>
        </div>
      ) : (
        <div>
          {enrollments.map((enrollment) => (
            <EnrollmentRow
              key={enrollment.id}
              enrollment={enrollment}
              seriesId={seriesId}
              isPossibleDuplicate={duplicateIds.has(enrollment.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Page Skeleton ────────────────────────────────────────────────────────────

function PageSkeleton() {
  return (
    <div className="space-y-4 animate-pulse">
      <div className="bg-white rounded-xl p-6">
        <div className="h-6 bg-gray-200 rounded w-1/3 mb-3" />
        <div className="h-4 bg-gray-100 rounded w-1/4 mb-2" />
        <div className="flex gap-2 mt-4">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-6 bg-gray-100 rounded-full w-20" />
          ))}
        </div>
      </div>
      <div className="bg-white rounded-xl p-6 space-y-3">
        <div className="h-5 bg-gray-200 rounded w-1/4" />
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-12 bg-gray-100 rounded-lg" />
        ))}
      </div>
    </div>
  );
}

// ─── Page ────────────────────────────────────────────────────────────────────

export default function LessonSeriesDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    setIsAdmin(getAuthUser()?.role === "Admin");
  }, []);

  const {
    data: series,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["lessonSeries", id],
    queryFn: () => getLessonSeriesById(id),
  });

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const deleteSeriesMutation = useMutation({
    mutationFn: () => deleteLessonSeries(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["lessonSeries"] });
      router.push("/dashboard/lessons");
    },
  });

  const exportMutation = useMutation({
    mutationFn: () => exportSeriePlanning(id),
    onSuccess: (blob) => {
      downloadBlob(blob, `${series?.name ?? "lessenreeks"}-planning.xlsx`);
      toast.success("Planning geëxporteerd");
    },
    onError: () => {
      toast.error("Kon de planning niet exporteren");
    },
  });

  return (
    <>
      {/* Back */}
      <Link
        href="/dashboard/lessons"
        className="inline-flex items-center gap-1.5 text-sm text-gray-400 hover:text-gray-600 transition-colors mb-6"
      >
        <ChevronLeft size={15} />
        Terug naar lessen
      </Link>

      {isLoading && <PageSkeleton />}

      {isError && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-5 text-sm text-red-600">
          Kon de lesreeks niet laden.{" "}
          <Link href="/dashboard/lessons" className="underline font-medium">
            Terug naar overzicht
          </Link>
        </div>
      )}

      {series && (
        <div className="space-y-5">
          {/* ── Section 1: Series info card ── */}
          <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2.5 flex-wrap mb-1.5">
                  <h1 className="text-xl font-bold text-gray-900 leading-tight">
                    {series.name}
                  </h1>
                  <span
                    className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                      series.isActive
                        ? "bg-green-50 text-green-700"
                        : "bg-gray-100 text-gray-500"
                    }`}
                  >
                    {series.isActive ? "Actief" : "Inactief"}
                  </span>
                </div>
                {series.description && (
                  <p className="text-sm text-gray-500 mb-3">
                    {series.description}
                  </p>
                )}

                {!editing && (
                  <div className="flex flex-wrap gap-2">
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
                      <Euro size={11} className="text-tennis-green" />
                      {series.price}
                    </span>
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
                      <CalendarDays size={11} className="text-tennis-green" />
                      {formatDateNL(series.startDate)} – {formatDateNL(series.endDate)}
                    </span>
                    {series.tennisClubName && (
                      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
                        <Building2 size={11} className="text-tennis-green" />
                        {series.tennisClubName}
                      </span>
                    )}
                  </div>
                )}
              </div>

              {!editing ? (
                <div className="flex items-center gap-2 shrink-0">
                  <Link
                    href={`/dashboard/lessons/${id}/planning`}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-tennis-green text-white text-xs font-medium hover:bg-tennis-green/90 transition-colors"
                  >
                    <CalendarDays size={12} />
                    Plan lessen
                  </Link>
                  <button
                    onClick={() => exportMutation.mutate()}
                    disabled={exportMutation.isPending}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
                  >
                    <Download size={12} />
                    {exportMutation.isPending ? "Exporteren…" : "Exporteer naar Excel"}
                  </button>
                  {isAdmin && (
                    <button
                      onClick={() => setEditing(true)}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors"
                    >
                      <Pencil size={12} />
                      Bewerken
                    </button>
                  )}
                </div>
              ) : (
                <button
                  onClick={() => setEditing(false)}
                  className="shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-500 hover:bg-gray-50 transition-colors"
                >
                  <X size={12} />
                  Sluiten
                </button>
              )}
            </div>

            {isAdmin && editing && (
              <EditSeriesForm
                seriesId={id}
                defaultValues={{
                  name: series.name,
                  description: series.description ?? "",
                  tennisClubId: series.tennisClubId,
                  price: series.price,
                  registrationDeadline:
                    series.registrationDeadline?.split("T")[0] ?? "",
                  isActive: series.isActive,
                  minAge: series.minAge ?? 3,
                  maxAge: series.maxAge ?? 99,
                  allowSoloEnrollment: series.allowSoloEnrollment,
                  allowGroupEnrollment: series.allowGroupEnrollment,
                  acceptOnlinePayment: series.acceptOnlinePayment,
                  acceptManualPayment: series.acceptManualPayment,
                }}
                onCancel={() => setEditing(false)}
                onSaved={() => setEditing(false)}
              />
            )}
          </div>

          {/* ── Section 2: Lesmomenten (paginated week view) ── */}
          <LessonWeekView
            lessons={series.lessons}
            startDate={series.startDate}
            endDate={series.endDate}
            trainers={trainers}
            seriesId={id}
          />

          {/* ── Section 4: Prijzen per categorie en groepsgrootte ── */}
          <PriceMatrixSection seriesId={id} legacyPrice={series.price} />

          {/* ── Section 5: Form builder ── */}
          <FormBuilderSection seriesId={id} />

          {/* ── Section 5: Enrollments ── */}
          <EnrollmentsSection seriesId={id} />

          {/* ── Section 6: Danger zone (admin only) ── */}
          {isAdmin && (
          <div className="border border-red-200 rounded-xl bg-red-50/30 p-5">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-red-100 flex items-center justify-center shrink-0 mt-0.5">
                <AlertTriangle size={15} className="text-red-500" />
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="text-sm font-semibold text-gray-800 mb-1">
                  Lesreeks verwijderen
                </h3>
                <p className="text-xs text-gray-500 mb-4 leading-relaxed">
                  Deze actie verwijdert de lesreeks inclusief alle lesmomenten
                  permanent. Dit is niet mogelijk als er actieve inschrijvingen
                  zijn.
                </p>
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <button className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white text-xs font-semibold rounded-lg hover:bg-red-700 transition-colors">
                      <Trash2 size={13} />
                      Lesreeks verwijderen
                    </button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>Lesreeks verwijderen?</AlertDialogTitle>
                      <AlertDialogDescription>
                        <strong>&ldquo;{series.name}&rdquo;</strong> en alle
                        bijhorende lesmomenten worden permanent verwijderd. Dit
                        kan niet ongedaan worden gemaakt.
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Annuleren</AlertDialogCancel>
                      <AlertDialogAction
                        onClick={() => deleteSeriesMutation.mutate()}
                        disabled={deleteSeriesMutation.isPending}
                        className="bg-red-600 hover:bg-red-700 text-white"
                      >
                        {deleteSeriesMutation.isPending
                          ? "Verwijderen..."
                          : "Definitief verwijderen"}
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>

                {deleteSeriesMutation.isError && (
                  <p className="text-xs text-red-600 mt-3">
                    Verwijderen mislukt. Mogelijk zijn er nog actieve
                    inschrijvingen.
                  </p>
                )}
              </div>
            </div>
          </div>
          )}
        </div>
      )}
    </>
  );
}
