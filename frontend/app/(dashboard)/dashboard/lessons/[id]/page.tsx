"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  ChevronLeft,
  Pencil,
  X,
  Trash2,
  Plus,
  Clock,
  MapPin,
  CalendarDays,
  Euro,
  UserCheck,
  BarChart2,
  AlertTriangle,
  Building2,
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
  createLesson,
  deleteLesson,
  LESSON_LEVELS,
  LessonDto,
} from "@/lib/api/lessonSeries";
import { getTrainers } from "@/lib/api/trainers";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";

// ─── Edit Series Form ─────────────────────────────────────────────────────────

const editSchema = z.object({
  name: z.string().min(1, "Naam is verplicht").max(200),
  description: z.string().max(1000).optional(),
  trainerId: z.string().min(1, "Trainer is verplicht"),
  tennisClubId: z.string().min(1, "Tennisclub is verplicht"),
  level: z.string().min(1, "Niveau is verplicht"),
  price: z.number().min(0),
  isActive: z.boolean(),
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
  const { data: members } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });
  const { data: tennisClubs } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const mutation = useMutation({
    mutationFn: (data: EditFormValues) =>
      updateLessonSeries(seriesId, {
        trainerId: data.trainerId,
        tennisClubId: data.tennisClubId,
        name: data.name,
        description: data.description,
        level: parseInt(data.level),
        price: data.price,
        isActive: data.isActive,
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
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    defaultValues,
  });

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
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Trainer
            </label>
            <Controller
              control={control}
              name="trainerId"
              render={({ field }) => (
                <Select onValueChange={field.onChange} value={field.value}>
                  <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm">
                    <SelectValue placeholder="Kies trainer" />
                  </SelectTrigger>
                  <SelectContent>
                    {members?.map((m) => (
                      <SelectItem key={m.id} value={m.id}>
                        {m.firstName} {m.lastName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            <FieldError message={errors.trainerId?.message} />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Niveau
            </label>
            <Controller
              control={control}
              name="level"
              render={({ field }) => (
                <Select onValueChange={field.onChange} value={field.value}>
                  <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm">
                    <SelectValue placeholder="Niveau" />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(LESSON_LEVELS).map(([v, l]) => (
                      <SelectItem key={v} value={v}>
                        {l}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
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
      </div>

      {mutation.isError && (
        <p className="text-xs text-red-500">Er ging iets mis. Probeer opnieuw.</p>
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

// ─── Lesson Row ───────────────────────────────────────────────────────────────

function LessonRow({
  lesson,
  seriesId,
  onDelete,
}: {
  lesson: LessonDto;
  seriesId: string;
  onDelete: () => void;
}) {
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: () => deleteLesson(seriesId, lesson.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      onDelete();
    },
  });

  return (
    <div
      className={`flex items-center justify-between gap-4 p-4 rounded-lg border ${
        lesson.isCancelled
          ? "bg-red-50/50 border-red-100"
          : "bg-white border-gray-100"
      }`}
    >
      <div className="flex items-center gap-4 flex-1 min-w-0">
        <div className="flex items-center gap-1.5 shrink-0">
          <CalendarDays size={13} className="text-tennis-green" />
          <span className="text-sm font-semibold text-gray-800">
            {lesson.date}
          </span>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          <Clock size={12} className="text-gray-400" />
          <span className="text-xs text-gray-500">
            {lesson.startTime}–{lesson.endTime}
          </span>
        </div>
        <div className="flex items-center gap-1 min-w-0">
          <MapPin size={12} className="text-gray-400 shrink-0" />
          <span className="text-xs text-gray-600 truncate">
            {lesson.courtName}
          </span>
        </div>
        {lesson.notes && (
          <span className="text-xs text-gray-400 truncate hidden md:block">
            {lesson.notes}
          </span>
        )}
        {lesson.isCancelled && (
          <span className="shrink-0 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-600">
            Geannuleerd
          </span>
        )}
      </div>

      <AlertDialog>
        <AlertDialogTrigger asChild>
          <button className="shrink-0 w-7 h-7 flex items-center justify-center rounded-lg text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors">
            <Trash2 size={13} />
          </button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Lesmoment verwijderen?</AlertDialogTitle>
            <AlertDialogDescription>
              Het lesmoment op {lesson.date} om {lesson.startTime} wordt
              permanent verwijderd. Dit kan niet ongedaan worden gemaakt.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annuleren</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteMutation.mutate()}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              {deleteMutation.isPending ? "Verwijderen..." : "Verwijderen"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Add Lesson Form ──────────────────────────────────────────────────────────

function makeLessonSchema(minDate: string, maxDate: string) {
  return z.object({
    date: z
      .string()
      .min(1, "Datum is verplicht")
      .refine((d) => d >= minDate && d <= maxDate, {
        message: `Datum moet tussen ${minDate} en ${maxDate} liggen`,
      }),
    startTime: z.string().min(1, "Starttijd is verplicht"),
    courtName: z.string().min(1, "Baan is verplicht").max(100),
    notes: z.string().optional(),
  });
}

type LessonFormValues = {
  date: string;
  startTime: string;
  courtName: string;
  notes?: string;
};

function AddLessonForm({
  seriesId,
  minDate,
  maxDate,
}: {
  seriesId: string;
  minDate: string;
  maxDate: string;
}) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (data: LessonFormValues) =>
      createLesson(seriesId, {
        date: data.date,
        startTime: data.startTime,
        courtName: data.courtName,
        notes: data.notes || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      reset();
    },
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<LessonFormValues>({
    resolver: zodResolver(makeLessonSchema(minDate, maxDate)),
  });

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-2">
        <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
          <Plus size={13} className="text-tennis-green" />
        </div>
        <h3 className="text-sm font-semibold text-gray-800">
          Lesmoment toevoegen
        </h3>
      </div>

      <form
        onSubmit={handleSubmit((d) => mutation.mutate(d))}
        className="p-5 space-y-4"
      >
        <div className="grid grid-cols-3 gap-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Datum
            </label>
            <input
              {...register("date")}
              type="date"
              min={minDate}
              max={maxDate}
              className={inputClass}
            />
            <FieldError message={errors.date?.message} />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Starttijd
            </label>
            <input
              {...register("startTime")}
              type="time"
              className={inputClass}
            />
            <FieldError message={errors.startTime?.message} />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Baan
            </label>
            <input
              {...register("courtName")}
              type="text"
              placeholder="Baan 1"
              className={inputClass}
            />
            <FieldError message={errors.courtName?.message} />
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">
            Notities (optioneel)
          </label>
          <input
            {...register("notes")}
            type="text"
            placeholder="Optionele notitie..."
            className={inputClass}
          />
        </div>

        {mutation.isError && (
          <p className="text-xs text-red-500">
            Er ging iets mis. Controleer de gegevens.
          </p>
        )}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="flex items-center gap-2 px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
        >
          {mutation.isPending ? (
            <span className="w-3.5 h-3.5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
          ) : (
            <Plus size={13} />
          )}
          {mutation.isPending ? "Toevoegen..." : "Toevoegen"}
        </button>
      </form>
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

  const {
    data: series,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["lessonSeries", id],
    queryFn: () => getLessonSeriesById(id),
  });

  const deleteSeriesMutation = useMutation({
    mutationFn: () => deleteLessonSeries(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["lessonSeries"] });
      router.push("/dashboard/lessons");
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
                <div className="flex items-center gap-1 text-sm text-gray-500 mb-3">
                  <UserCheck size={13} />
                  <span>{series.trainerName || "—"}</span>
                  <span className="text-gray-300 mx-1">·</span>
                  <BarChart2 size={13} />
                  <span>{LESSON_LEVELS[series.level] ?? `N${series.level}`}</span>
                </div>
                {series.description && (
                  <p className="text-sm text-gray-500 mb-3">
                    {series.description}
                  </p>
                )}

                {!editing && (
                  <div className="flex flex-wrap gap-2">
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
                      <Euro size={11} className="text-tennis-green" />€
                      {series.price}
                    </span>
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
                      <Clock size={11} className="text-tennis-green" />
                      {series.durationMinutes} min
                    </span>
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
                      <CalendarDays size={11} className="text-tennis-green" />
                      {series.startDate} – {series.endDate}
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
                <button
                  onClick={() => setEditing(true)}
                  className="shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors"
                >
                  <Pencil size={12} />
                  Bewerken
                </button>
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

            {editing && (
              <EditSeriesForm
                seriesId={id}
                defaultValues={{
                  name: series.name,
                  description: series.description ?? "",
                  trainerId: series.trainerId,
                  tennisClubId: series.tennisClubId,
                  level: String(series.level),
                  price: series.price,
                  isActive: series.isActive,
                }}
                onCancel={() => setEditing(false)}
                onSaved={() => setEditing(false)}
              />
            )}
          </div>

          {/* ── Section 2: Lesmomenten ── */}
          <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
            <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-2.5">
              <h2 className="text-sm font-semibold text-gray-800">
                Lesmomenten
              </h2>
              <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-tennis-green/10 text-tennis-green text-xs font-bold">
                {series.lessons.length}
              </span>
            </div>

            {series.lessons.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <p className="text-sm text-gray-400 font-medium">
                  Nog geen lesmomenten
                </p>
                <p className="text-xs text-gray-300 mt-1">
                  Gebruik het formulier hieronder om een lesmoment toe te voegen.
                </p>
              </div>
            ) : (
              <div className="p-4 space-y-2">
                {series.lessons.map((lesson) => (
                  <LessonRow
                    key={lesson.id}
                    lesson={lesson}
                    seriesId={id}
                    onDelete={() => {}}
                  />
                ))}
              </div>
            )}
          </div>

          {/* ── Section 3: Add lesson ── */}
          <AddLessonForm
            seriesId={id}
            minDate={series.startDate}
            maxDate={series.endDate}
          />

          {/* ── Section 4: Danger zone ── */}
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
        </div>
      )}
    </>
  );
}
