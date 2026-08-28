"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { ChevronLeft } from "lucide-react";
import * as z from "zod";
import {
  createStandaloneLesson,
  ALLOWED_DURATIONS,
} from "@/lib/api/standaloneLessons";
import { getTrainers, isAssignableTrainer } from "@/lib/api/trainers";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { DatePicker } from "@/components/ui/date-picker";
import { NativeSelect } from "@/components/ui/native-select";
import { FieldError } from "@/components/forms/field-error";
import { EmailTagInput } from "@/components/forms/email-tag-input";
import { SlashLabel } from "@/components/ui/slash-label";
import { inputClass } from "@/lib/styles";

const schema = z.object({
  date: z.string().min(1, "Datum is verplicht"),
  startTime: z
    .string()
    .regex(/^\d{2}:\d{2}$/, "Tijd moet HH:mm zijn"),
  durationMinutes: z
    .number()
    .refine((v) => ALLOWED_DURATIONS.includes(v as typeof ALLOWED_DURATIONS[number]), {
      message: "Ongeldige duur",
    }),
  courtName: z.string().min(1, "Baan is verplicht").max(100),
  tennisClubId: z.string().min(1, "Club is verplicht"),
  level: z.number().int().min(1).max(3).nullable(),
  trainerId: z.string().min(1, "Trainer is verplicht"),
  maxParticipants: z.number().int().min(0, "Mag niet negatief zijn"),
  notes: z.string().max(1000).optional().nullable(),
  participantEmails: z
    .array(z.string().email())
    .min(1, "Minstens één deelnemer is verplicht"),
});

type FormValues = z.infer<typeof schema>;

function Label({
  children,
  required,
}: {
  children: React.ReactNode;
  required?: boolean;
}) {
  return (
    <label className="block text-[11.5px] font-semibold text-ink mb-1.5">
      {children}
      {required && <span className="text-tennis-green ml-0.5">*</span>}
    </label>
  );
}

export default function NewStandaloneLessonPage() {
  const t = useTranslations("standaloneLessons");
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: trainers, isLoading: trainersLoading } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const { data: tennisClubs, isLoading: clubsLoading } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const assignableTrainers = (trainers ?? []).filter(isAssignableTrainer);

  const {
    register,
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      date: "",
      startTime: "",
      durationMinutes: 60,
      courtName: "",
      tennisClubId: "",
      level: null,
      trainerId: "",
      maxParticipants: 4,
      notes: "",
      participantEmails: [],
    },
  });

  const createMutation = useMutation({
    mutationFn: createStandaloneLesson,
    onSuccess: (id) => {
      queryClient.invalidateQueries({ queryKey: ["standaloneLessons"] });
      toast.success(t("createSuccess"));
      router.push(`/dashboard/standalone-lessons/${id}`);
    },
  });

  async function onSubmit(values: FormValues) {
    await createMutation.mutateAsync({
      date: values.date,
      startTime: values.startTime,
      durationMinutes: values.durationMinutes,
      courtName: values.courtName,
      tennisClubId: values.tennisClubId,
      level: values.level,
      trainerId: values.trainerId,
      maxParticipants: values.maxParticipants,
      notes: values.notes && values.notes.trim() ? values.notes : null,
      participantEmails: values.participantEmails,
    });
  }

  return (
    <>
      {/* Page header */}
      <div className="mb-5">
        <Link
          href="/dashboard/standalone-lessons"
          className="inline-flex items-center gap-1 text-[11.5px] text-ink-3 hover:text-ink mb-2"
        >
          <ChevronLeft size={14} /> {t("back")}
        </Link>
        <SlashLabel>/nieuwe-les</SlashLabel>
        <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">
          {t("createTitle")}
        </h1>
        <p className="text-[12.5px] text-ink-3 mt-0.5">
          {t("createSubtitle")}
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="bg-paper border border-rule rounded-xl p-6 space-y-5 max-w-[680px]">
          {/* Row 1: Date + Start time */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label required>{t("fieldDate")}</Label>
              <Controller
                control={control}
                name="date"
                render={({ field }) => (
                  <DatePicker value={field.value} onChange={field.onChange} />
                )}
              />
              <FieldError message={errors.date?.message} />
            </div>
            <div>
              <Label required>{t("fieldStartTime")}</Label>
              <input
                {...register("startTime")}
                type="time"
                step={300}
                className={inputClass}
              />
              <FieldError message={errors.startTime?.message} />
            </div>
          </div>

          {/* Row 2: Duration + Court */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label required>{t("fieldDuration")}</Label>
              <Controller
                control={control}
                name="durationMinutes"
                render={({ field }) => (
                  <NativeSelect
                    value={field.value}
                    onChange={(e) => field.onChange(Number(e.target.value))}
                  >
                    {ALLOWED_DURATIONS.map((d) => (
                      <option key={d} value={d}>
                        {t("fieldDurationMinutes", { minutes: d })}
                      </option>
                    ))}
                  </NativeSelect>
                )}
              />
              <FieldError message={errors.durationMinutes?.message} />
            </div>
            <div>
              <Label required>{t("fieldCourt")}</Label>
              <input
                {...register("courtName")}
                type="text"
                placeholder="Baan 1"
                className={inputClass}
              />
              <FieldError message={errors.courtName?.message} />
            </div>
          </div>

          {/* Row 2b: Club */}
          <div>
            <Label required>{t("fieldClub")}</Label>
            {clubsLoading ? (
              <div className="h-10 rounded-lg bg-canvas animate-pulse" />
            ) : (tennisClubs ?? []).length === 0 ? (
              <p className="text-[12px] text-amber-700 bg-amber-50 border border-amber-100 rounded-lg px-3 py-2">
                {t("noClubs")}
              </p>
            ) : (
              <Controller
                control={control}
                name="tennisClubId"
                render={({ field }) => (
                  <NativeSelect
                    value={field.value}
                    onChange={(e) => field.onChange(e.target.value)}
                  >
                    <option value="">{t("selectClub")}</option>
                    {(tennisClubs ?? []).map((club) => (
                      <option key={club.id} value={club.id}>
                        {club.name}
                      </option>
                    ))}
                  </NativeSelect>
                )}
              />
            )}
            <FieldError message={errors.tennisClubId?.message} />
          </div>

          {/* Row 3: Level + Max participants */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>{t("fieldLevel")}</Label>
              <Controller
                control={control}
                name="level"
                render={({ field }) => (
                  <NativeSelect
                    value={field.value ?? ""}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value === "" ? null : Number(e.target.value)
                      )
                    }
                  >
                    <option value="">{t("selectLevel")}</option>
                    <option value={1}>{t("level1")}</option>
                    <option value={2}>{t("level2")}</option>
                    <option value={3}>{t("level3")}</option>
                  </NativeSelect>
                )}
              />
              <FieldError message={errors.level?.message} />
            </div>
            <div>
              <Label>{t("fieldMaxParticipants")}</Label>
              <input
                {...register("maxParticipants", { valueAsNumber: true })}
                type="number"
                min={0}
                className={inputClass}
              />
              <p className="mt-1 text-[10.5px] text-ink-3">
                {t("fieldMaxParticipantsHint")}
              </p>
              <FieldError message={errors.maxParticipants?.message} />
            </div>
          </div>

          {/* Row 4: Trainer (full width) */}
          <div>
            <Label required>{t("fieldTrainer")}</Label>
            {trainersLoading ? (
              <div className="h-10 rounded-lg bg-canvas animate-pulse" />
            ) : assignableTrainers.length === 0 ? (
              <p className="text-[12px] text-amber-700 bg-amber-50 border border-amber-100 rounded-lg px-3 py-2">
                {t("noTrainers")}
              </p>
            ) : (
              <Controller
                control={control}
                name="trainerId"
                render={({ field }) => (
                  <NativeSelect
                    value={field.value}
                    onChange={(e) => field.onChange(e.target.value)}
                  >
                    <option value="">{t("selectTrainer")}</option>
                    {assignableTrainers.map((tr) => (
                      <option key={tr.id} value={tr.id}>
                        {tr.firstName} {tr.lastName}
                      </option>
                    ))}
                  </NativeSelect>
                )}
              />
            )}
            <FieldError message={errors.trainerId?.message} />
          </div>

          {/* Notes */}
          <div>
            <Label>{t("fieldNotes")}</Label>
            <textarea
              {...register("notes")}
              rows={3}
              placeholder={t("fieldNotesPlaceholder")}
              className={inputClass + " resize-y"}
            />
            <FieldError message={errors.notes?.message} />
          </div>

          {/* Participant emails */}
          <div>
            <Label required>{t("fieldEmails")}</Label>
            <Controller
              control={control}
              name="participantEmails"
              render={({ field }) => (
                <EmailTagInput
                  value={field.value}
                  onChange={field.onChange}
                  placeholder={t("fieldEmailsPlaceholder")}
                  hasError={Boolean(errors.participantEmails)}
                />
              )}
            />
            <p className="mt-1 text-[10.5px] text-ink-3">
              {t("fieldEmailsHint")}
            </p>
            <FieldError message={errors.participantEmails?.message} />
          </div>
        </div>

        {/* Submit row */}
        <div className="max-w-[680px] mt-5 flex justify-end">
          <button
            type="submit"
            disabled={isSubmitting || createMutation.isPending}
            className="inline-flex items-center gap-1.5 px-4 py-2 bg-tennis-green text-tennis-lime text-[12.5px] font-semibold rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting || createMutation.isPending
              ? t("scheduling")
              : t("scheduleAndInvite")}
          </button>
        </div>
      </form>
    </>
  );
}
