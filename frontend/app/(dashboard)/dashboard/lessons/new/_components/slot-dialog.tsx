"use client";

import { useTranslations } from "next-intl";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";
import { LESSON_LEVELS } from "@/lib/api/lessonSeries";
import type { TrainerDto } from "@/lib/api/trainers";
import type { WizardSlot } from "../_types";

const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

const schema = z
  .object({
    trainerId: z.string().min(1, "Trainer is verplicht"),
    startTime: z.string().min(1, "Starttijd is verplicht"),
    endTime: z.string().min(1, "Eindtijd is verplicht"),
    courtName: z.string().min(1, "Naam baan is verplicht"),
    maxStudents: z
      .number()
      .min(1, "Minimaal 1 leerling")
      .max(4, "Maximaal 4 leerlingen"),
    level: z.string(), // "none" = no level
  })
  .refine((d) => !d.startTime || !d.endTime || d.endTime > d.startTime, {
    message: "Eindtijd moet na starttijd zijn",
    path: ["endTime"],
  });

type FormValues = z.infer<typeof schema>;

interface SlotDialogProps {
  open: boolean;
  dayOfWeek: number;
  trainers: TrainerDto[];
  defaultStartTime?: string;
  defaultEndTime?: string;
  onSave: (slot: Omit<WizardSlot, "id">) => void;
  onClose: () => void;
}

function Label({
  children,
  required,
}: {
  children: React.ReactNode;
  required?: boolean;
}) {
  return (
    <label className="block text-sm font-medium text-gray-700 mb-1.5">
      {children}
      {required && <span className="text-red-400 ml-0.5">*</span>}
    </label>
  );
}

export function SlotDialog({
  open,
  dayOfWeek,
  trainers,
  defaultStartTime,
  defaultEndTime,
  onSave,
  onClose,
}: SlotDialogProps) {
  const t = useTranslations("lessonWizard");

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      maxStudents: 4,
      level: "none",
      startTime: defaultStartTime ?? "",
      endTime: defaultEndTime ?? "",
    },
  });

  function onSubmit(data: FormValues) {
    const trainer = trainers.find((tr) => tr.id === data.trainerId);
    onSave({
      dayOfWeek,
      startTime: data.startTime,
      endTime: data.endTime,
      trainerId: data.trainerId,
      trainerName: trainer ? `${trainer.firstName} ${trainer.lastName}` : "",
      courtName: data.courtName,
      maxStudents: data.maxStudents,
      level: data.level && data.level !== "none" ? parseInt(data.level) : null,
    });
    reset({ maxStudents: 4, level: "none" });
    onClose();
  }

  function handleClose() {
    reset({ maxStudents: 4, level: "none" });
    onClose();
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && handleClose()}>
      <DialogContent className="max-w-md" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle>
            {t("slotDialogTitle")} — {DAY_NAMES_FULL[dayOfWeek]}
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          {/* Trainer */}
          <div>
            <Label required>{t("trainer")}</Label>
            <Controller
              control={control}
              name="trainerId"
              render={({ field }) => (
                <Select
                  onValueChange={field.onChange}
                  value={field.value ?? ""}
                >
                  <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                    <SelectValue placeholder={t("trainerPlaceholder")} />
                  </SelectTrigger>
                  <SelectContent>
                    {trainers.map((tr) => (
                      <SelectItem key={tr.id} value={tr.id}>
                        {tr.firstName} {tr.lastName}
                      </SelectItem>
                    ))}
                    {!trainers.length && (
                      <div className="px-3 py-2 text-sm text-gray-400">
                        {t("noTrainersFound")}
                      </div>
                    )}
                  </SelectContent>
                </Select>
              )}
            />
            <FieldError message={errors.trainerId?.message} />
          </div>

          {/* Start / End time */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label required>{t("startTime")}</Label>
              <input
                {...register("startTime")}
                type="time"
                className={inputClass}
              />
              <FieldError message={errors.startTime?.message} />
            </div>
            <div>
              <Label required>{t("endTime")}</Label>
              <input
                {...register("endTime")}
                type="time"
                className={inputClass}
              />
              <FieldError message={errors.endTime?.message} />
            </div>
          </div>

          {/* Court name */}
          <div>
            <Label required>{t("courtName")}</Label>
            <input
              {...register("courtName")}
              type="text"
              placeholder={t("courtNamePlaceholder")}
              className={inputClass}
            />
            <FieldError message={errors.courtName?.message} />
          </div>

          {/* Max students + Level */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label required>{t("maxStudents")}</Label>
              <input
                {...register("maxStudents", { valueAsNumber: true })}
                type="number"
                min={1}
                className={inputClass}
              />
              <FieldError message={errors.maxStudents?.message} />
            </div>
            <div>
              <Label>{t("level")}</Label>
              <Controller
                control={control}
                name="level"
                render={({ field }) => (
                  <Select
                    onValueChange={field.onChange}
                    value={field.value ?? ""}
                  >
                    <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                      <SelectValue placeholder={t("levelNone")} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">{t("levelNone")}</SelectItem>
                      {Object.entries(LESSON_LEVELS).map(([value, label]) => (
                        <SelectItem key={value} value={value}>
                          {label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-3 pt-2 border-t border-gray-100">
            <button
              type="button"
              onClick={handleClose}
              className="flex-1 px-4 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
            >
              {t("cancel")}
            </button>
            <button
              type="submit"
              className="flex-1 px-4 py-2 text-sm font-semibold text-white bg-tennis-green rounded-lg hover:bg-tennis-green/90 transition-colors"
            >
              {t("saveSlot")}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
