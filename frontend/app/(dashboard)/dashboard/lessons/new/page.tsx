"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { ChevronLeft, ChevronRight } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  createLessonSeries,
  LESSON_LEVELS,
} from "@/lib/api/lessonSeries";
import { getTrainers } from "@/lib/api/trainers";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";

// ─── Form Schema ──────────────────────────────────────────────────────────────

const schema = z.object({
  trainerId: z.string().min(1, "Trainer is verplicht"),
  tennisClubId: z.string().min(1, "Tennisclub is verplicht"),
  name: z.string().min(1, "Naam is verplicht").max(200, "Naam mag maximaal 200 tekens zijn"),
  description: z.string().max(1000, "Omschrijving mag maximaal 1000 tekens zijn").optional(),
  level: z.string().min(1, "Niveau is verplicht"),
  price: z.number().min(0, "Prijs mag niet negatief zijn"),
  durationMinutes: z.number().min(1, "Duur moet minimaal 1 minuut zijn"),
  startDate: z.string().min(1, "Startdatum is verplicht"),
  endDate: z.string().min(1, "Einddatum is verplicht"),
});

type FormValues = z.infer<typeof schema>;

function Label({ children, required }: { children: React.ReactNode; required?: boolean }) {
  return (
    <label className="block text-sm font-medium text-gray-700 mb-1.5">
      {children}
      {required && <span className="text-red-400 ml-0.5">*</span>}
    </label>
  );
}

// ─── Page ────────────────────────────────────────────────────────────────────

export default function NewLessonSeriesPage() {
  const t = useTranslations("lessonSeries");
  const router = useRouter();

  const { data: members, isLoading: membersLoading } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const { data: tennisClubs, isLoading: clubsLoading } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const mutation = useMutation({
    mutationFn: createLessonSeries,
    onSuccess: () => router.push("/dashboard/lessons"),
  });

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      price: 0,
      durationMinutes: 60,
    },
  });

  function onSubmit(data: FormValues) {
    mutation.mutate({
      trainerId: data.trainerId,
      tennisClubId: data.tennisClubId,
      name: data.name,
      description: data.description || undefined,
      level: parseInt(data.level),
      price: data.price,
      durationMinutes: data.durationMinutes,
      startDate: data.startDate,
      endDate: data.endDate,
    });
  }

  return (
    <div className="max-w-2xl mx-auto">
      {/* Back */}
      <Link
        href="/dashboard/lessons"
        className="inline-flex items-center gap-1.5 text-sm text-gray-400 hover:text-gray-600 transition-colors mb-6"
      >
        <ChevronLeft size={15} />
        Terug naar lessen
      </Link>

      {/* Page title */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          Nieuwe lesreeks
        </h1>
        <p className="text-gray-400 text-sm mt-1">
          Stel een nieuwe lesreeks in en voeg daarna lesmomenten toe.
        </p>
      </div>

      {/* Error banner */}
      {mutation.isError && (
        <div className="mb-5 bg-red-50 border border-red-100 rounded-lg p-4 text-sm text-red-600">
          Er ging iets mis. Controleer je gegevens en probeer opnieuw.
        </div>
      )}

      {/* Form card */}
      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6 space-y-5">
          {/* Naam */}
          <div>
            <Label required>Naam</Label>
            <input
              {...register("name")}
              type="text"
              placeholder="Voorjaarslessen 2026"
              className={inputClass}
            />
            <FieldError message={errors.name?.message} />
          </div>

          {/* Omschrijving */}
          <div>
            <Label>Omschrijving</Label>
            <textarea
              {...register("description")}
              rows={3}
              placeholder="Optionele omschrijving van de lesreeks..."
              className={inputClass + " resize-none"}
            />
            <FieldError message={errors.description?.message} />
          </div>

          {/* Trainer */}
          <div>
            <Label required>Trainer</Label>
            <Controller
              control={control}
              name="trainerId"
              render={({ field }) => (
                <Select
                  onValueChange={field.onChange}
                  value={field.value}
                  disabled={membersLoading}
                >
                  <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                    <SelectValue
                      placeholder={membersLoading ? "Laden..." : "Kies een trainer"}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    {members?.map((m) => (
                      <SelectItem key={m.id} value={m.id}>
                        {m.firstName} {m.lastName}
                      </SelectItem>
                    ))}
                    {!membersLoading && !members?.length && (
                      <div className="px-3 py-2 text-sm text-gray-400">
                        Geen leden gevonden
                      </div>
                    )}
                  </SelectContent>
                </Select>
              )}
            />
            <FieldError message={errors.trainerId?.message} />
          </div>

          {/* Tennisclub */}
          <div>
            <Label required>Tennisclub</Label>
            <Controller
              control={control}
              name="tennisClubId"
              render={({ field }) => (
                <Select
                  onValueChange={field.onChange}
                  value={field.value}
                  disabled={clubsLoading}
                >
                  <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                    <SelectValue
                      placeholder={clubsLoading ? "Laden..." : "Kies een tennisclub"}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    {tennisClubs?.map((c) => (
                      <SelectItem key={c.id} value={c.id}>
                        {c.name}
                      </SelectItem>
                    ))}
                    {!clubsLoading && !tennisClubs?.length && (
                      <div className="px-3 py-2 text-sm text-gray-400">
                        Geen clubs gevonden
                      </div>
                    )}
                  </SelectContent>
                </Select>
              )}
            />
            <FieldError message={errors.tennisClubId?.message} />
          </div>

          {/* Niveau */}
          <div>
            <Label required>Niveau</Label>
            <Controller
              control={control}
              name="level"
              render={({ field }) => (
                <Select onValueChange={field.onChange} value={field.value}>
                  <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                    <SelectValue placeholder="Kies een niveau" />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(LESSON_LEVELS).map(([value, label]) => (
                      <SelectItem key={value} value={value}>
                        {label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            <FieldError message={errors.level?.message} />
          </div>

          {/* Prijs */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label required>Prijs</Label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-gray-400 pointer-events-none">
                  €
                </span>
                <input
                  {...register("price", { valueAsNumber: true })}
                  type="number"
                  min={0}
                  step={0.01}
                  className={inputClass + " pl-7"}
                />
              </div>
              <FieldError message={errors.price?.message} />
            </div>
          </div>

          {/* Duur */}
          <div>
            <Label required>Duur (minuten)</Label>
            <input
              {...register("durationMinutes", { valueAsNumber: true })}
              type="number"
              min={1}
              className={inputClass}
            />
            <FieldError message={errors.durationMinutes?.message} />
          </div>

          {/* Startdatum + Einddatum */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label required>Startdatum</Label>
              <input
                {...register("startDate")}
                type="date"
                className={inputClass}
              />
              <FieldError message={errors.startDate?.message} />
            </div>
            <div>
              <Label required>Einddatum</Label>
              <input
                {...register("endDate")}
                type="date"
                className={inputClass}
              />
              <FieldError message={errors.endDate?.message} />
            </div>
          </div>

          <div className="pt-1 border-t border-gray-100" />

          {/* Submit */}
          <button
            type="submit"
            disabled={mutation.isPending}
            className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {mutation.isPending ? (
              <>
                <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                Aanmaken...
              </>
            ) : (
              <>
                Lesreeks aanmaken
                <ChevronRight size={15} />
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
