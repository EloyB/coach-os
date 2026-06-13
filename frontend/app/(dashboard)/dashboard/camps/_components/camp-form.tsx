"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { useTranslations } from "next-intl";
import { Trash2, Plus, Clock } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";
import { isAssignableTrainer, type TrainerDto } from "@/lib/api/trainers";
import type { TennisClubDto } from "@/lib/api/tennisClubs";
import type { CampDetailDto, CreateCampRequest } from "@/lib/api/camps";

// ─── Local day/trainer draft state ────────────────────────────────────────────

type TrainerDraft = { trainerId: string; startTime: string; endTime: string };

type DayDraft = {
  date: string; // yyyy-MM-dd
  startTime: string; // HH:mm
  endTime: string;
  trainers: TrainerDraft[];
};

/**
 * Build one DayDraft per date in [start, end]. Existing days (matched by date)
 * keep their times + trainers so adjusting the range does not wipe entered data.
 */
function buildDays(start: string, end: string, existing: DayDraft[]): DayDraft[] {
  if (!start || !end || end < start) return [];
  const byDate = new Map(existing.map((d) => [d.date, d]));
  const result: DayDraft[] = [];
  const cur = new Date(start + "T00:00:00");
  const last = new Date(end + "T00:00:00");
  while (cur <= last) {
    const iso = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, "0")}-${String(cur.getDate()).padStart(2, "0")}`;
    result.push(
      byDate.get(iso) ?? {
        date: iso,
        startTime: "09:00",
        endTime: "16:00",
        trainers: [],
      },
    );
    cur.setDate(cur.getDate() + 1);
  }
  return result;
}

function formatDayHeading(iso: string): string {
  const date = new Date(iso + "T00:00:00");
  return new Intl.DateTimeFormat("nl-BE", {
    weekday: "long",
    day: "numeric",
    month: "long",
  }).format(date);
}

/** Clamp a HH:mm value into [min, max] (lexicographic compare works for HH:mm). */
function clampTime(value: string, min: string, max: string): string {
  if (value < min) return min;
  if (value > max) return max;
  return value;
}

// ─── Zod schema for the basis fields ──────────────────────────────────────────

const schema = z.object({
  name: z.string().min(1),
  description: z.string().optional(),
  tennisClubId: z.string().min(1),
  level: z.number().nullable().optional(),
  price: z.number().min(0),
  registrationDeadline: z.string().min(1),
  maxParticipants: z.number().nullable().optional(),
  startDate: z.string().min(1),
  endDate: z.string().min(1),
});

type FormValues = z.infer<typeof schema>;

// ─── Helpers to map an existing camp into form state ──────────────────────────

function detailToDays(initial: CampDetailDto): DayDraft[] {
  return initial.days.map((d) => ({
    date: d.date,
    startTime: d.startTime,
    endTime: d.endTime,
    trainers: d.trainers.map((tr) => ({
      trainerId: tr.trainerId,
      startTime: tr.startTime,
      endTime: tr.endTime,
    })),
  }));
}

/** Convert an ISO timestamp to a value usable by <input type="datetime-local">. */
function toDateTimeLocal(iso: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

interface CampFormProps {
  initial?: CampDetailDto;
  clubs: TennisClubDto[];
  trainers: TrainerDto[];
  onSubmit: (req: CreateCampRequest) => void;
  submitting: boolean;
  errorMessages?: string[];
}

export function CampForm({
  initial,
  clubs,
  trainers,
  onSubmit,
  submitting,
  errorMessages,
}: CampFormProps) {
  const t = useTranslations("camps");

  const assignableTrainers = trainers.filter(isAssignableTrainer);

  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: initial?.name ?? "",
      description: initial?.description ?? "",
      tennisClubId: initial?.tennisClubId ?? "",
      level: initial?.level ?? null,
      price: initial?.price ?? 0,
      registrationDeadline: initial
        ? toDateTimeLocal(initial.registrationDeadline)
        : "",
      maxParticipants: initial?.maxParticipants ?? null,
      startDate: initial?.startDate ?? "",
      endDate: initial?.endDate ?? "",
    },
  });

  const [days, setDays] = useState<DayDraft[]>(
    initial ? detailToDays(initial) : [],
  );

  function regenerateDays(start: string, end: string) {
    setDays((prev) => buildDays(start, end, prev));
  }

  function updateDay(date: string, updates: Partial<DayDraft>) {
    setDays((prev) =>
      prev.map((d) => (d.date === date ? { ...d, ...updates } : d)),
    );
  }

  function addTrainer(date: string, trainerId: string) {
    if (!trainerId) return;
    setDays((prev) =>
      prev.map((d) => {
        if (d.date !== date) return d;
        if (d.trainers.some((tr) => tr.trainerId === trainerId)) return d;
        return {
          ...d,
          trainers: [
            ...d.trainers,
            { trainerId, startTime: d.startTime, endTime: d.endTime },
          ],
        };
      }),
    );
  }

  function removeTrainer(date: string, trainerId: string) {
    setDays((prev) =>
      prev.map((d) =>
        d.date === date
          ? { ...d, trainers: d.trainers.filter((tr) => tr.trainerId !== trainerId) }
          : d,
      ),
    );
  }

  function updateTrainerTime(
    date: string,
    trainerId: string,
    field: "startTime" | "endTime",
    value: string,
  ) {
    setDays((prev) =>
      prev.map((d) => {
        if (d.date !== date) return d;
        return {
          ...d,
          trainers: d.trainers.map((tr) =>
            tr.trainerId === trainerId
              ? { ...tr, [field]: clampTime(value, d.startTime, d.endTime) }
              : tr,
          ),
        };
      }),
    );
  }

  function trainerName(trainerId: string): string {
    const tr = trainers.find((x) => x.id === trainerId);
    return tr ? `${tr.firstName} ${tr.lastName}` : trainerId;
  }

  function submit(values: FormValues) {
    const req: CreateCampRequest = {
      name: values.name.trim(),
      description: values.description?.trim() || undefined,
      tennisClubId: values.tennisClubId,
      level: values.level ?? null,
      price: values.price,
      startDate: values.startDate,
      endDate: values.endDate,
      registrationDeadline: new Date(values.registrationDeadline).toISOString(),
      maxParticipants: values.maxParticipants ?? null,
      days: days.map((d) => ({
        date: d.date,
        startTime: d.startTime,
        endTime: d.endTime,
        trainers: d.trainers.map((tr) => ({
          trainerId: tr.trainerId,
          startTime: tr.startTime,
          endTime: tr.endTime,
        })),
      })),
    };
    onSubmit(req);
  }

  const labelCls = "block text-xs font-semibold text-ink-2 mb-1.5";
  const hintCls = "text-[11px] text-ink-3 mt-1";

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-5">
      {/* ── Basis ── */}
      <div className="bg-paper border border-rule rounded-xl overflow-hidden">
        <div className="px-5 py-4 border-b border-rule">
          <h2 className="text-sm font-semibold text-ink">{t("title")}</h2>
        </div>
        <div className="p-5 grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="md:col-span-2">
            <label className={labelCls}>{t("fieldName")}</label>
            <input
              type="text"
              placeholder={t("fieldNamePlaceholder")}
              className={inputClass}
              {...register("name")}
            />
            {errors.name && (
              <p className="text-[11px] text-red-500 mt-1">
                {t("requiredField")}
              </p>
            )}
          </div>

          <div className="md:col-span-2">
            <label className={labelCls}>{t("fieldDescription")}</label>
            <textarea
              rows={3}
              placeholder={t("fieldDescriptionPlaceholder")}
              className={inputClass + " resize-y"}
              {...register("description")}
            />
          </div>

          <div>
            <label className={labelCls}>{t("fieldClub")}</label>
            {clubs.length === 0 ? (
              <p className={hintCls}>{t("noClubs")}</p>
            ) : (
              <NativeSelect {...register("tennisClubId")} className="w-full">
                <option value="">{t("selectClub")}</option>
                {clubs.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </NativeSelect>
            )}
            {errors.tennisClubId && (
              <p className="text-[11px] text-red-500 mt-1">
                {t("requiredField")}
              </p>
            )}
          </div>

          <div>
            <label className={labelCls}>{t("fieldLevel")}</label>
            <NativeSelect
              className="w-full"
              {...register("level", {
                setValueAs: (v) => (v === "" ? null : Number(v)),
              })}
            >
              <option value="">{t("levelNone")}</option>
              <option value="1">{t("level1")}</option>
              <option value="2">{t("level2")}</option>
              <option value="3">{t("level3")}</option>
            </NativeSelect>
          </div>

          <div>
            <label className={labelCls}>{t("fieldPrice")}</label>
            <input
              type="number"
              step="0.01"
              min="0"
              className={inputClass}
              {...register("price", { valueAsNumber: true })}
            />
            <p className={hintCls}>{t("fieldPriceHint")}</p>
          </div>

          <div>
            <label className={labelCls}>{t("fieldMaxParticipants")}</label>
            <input
              type="number"
              min="1"
              className={inputClass}
              {...register("maxParticipants", {
                setValueAs: (v) =>
                  v === "" || v === null ? null : Number(v),
              })}
            />
            <p className={hintCls}>{t("fieldMaxParticipantsHint")}</p>
          </div>

          <div>
            <label className={labelCls}>{t("fieldRegistrationDeadline")}</label>
            <input
              type="datetime-local"
              className={inputClass}
              {...register("registrationDeadline")}
            />
            {errors.registrationDeadline && (
              <p className="text-[11px] text-red-500 mt-1">
                {t("requiredField")}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3 md:col-span-2">
            <div>
              <label className={labelCls}>{t("fieldStartDate")}</label>
              <input
                type="date"
                className={inputClass}
                {...register("startDate", {
                  onChange: (e) =>
                    regenerateDays(e.target.value, getValues("endDate")),
                })}
              />
              {errors.startDate && (
                <p className="text-[11px] text-red-500 mt-1">
                  {t("requiredField")}
                </p>
              )}
            </div>
            <div>
              <label className={labelCls}>{t("fieldEndDate")}</label>
              <input
                type="date"
                className={inputClass}
                {...register("endDate", {
                  onChange: (e) =>
                    regenerateDays(getValues("startDate"), e.target.value),
                })}
              />
              {errors.endDate && (
                <p className="text-[11px] text-red-500 mt-1">
                  {t("requiredField")}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ── Dagen & trainers ── */}
      <div className="bg-paper border border-rule rounded-xl overflow-hidden">
        <div className="px-5 py-4 border-b border-rule flex items-center gap-2.5">
          <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
            <Clock size={13} className="text-tennis-green" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-ink">{t("daysTitle")}</h2>
            <p className="text-[11px] text-ink-3 mt-0.5">
              {t("daysDescription")}
            </p>
          </div>
        </div>

        <div className="p-5 space-y-3">
          {days.length === 0 && (
            <p className="text-xs text-ink-3 py-2">{t("noDays")}</p>
          )}

          {days.map((day) => {
            const available = assignableTrainers.filter(
              (tr) => !day.trainers.some((d) => d.trainerId === tr.id),
            );
            return (
              <div
                key={day.date}
                className="border border-rule rounded-xl p-4 bg-canvas"
              >
                <div className="text-[13px] font-bold text-tennis-green mb-3">
                  {formatDayHeading(day.date)}
                </div>

                {/* Camp hours */}
                <p className="text-[10.5px] uppercase tracking-[0.04em] text-ink-3 mb-1.5">
                  {t("dayHours")}
                </p>
                <div className="flex items-center gap-2 mb-3 pb-3 border-b border-dashed border-rule">
                  <input
                    type="time"
                    value={day.startTime}
                    onChange={(e) =>
                      updateDay(day.date, { startTime: e.target.value })
                    }
                    className="border border-rule rounded-lg px-2.5 py-1.5 text-sm bg-white focus:outline-none focus:border-tennis-green"
                  />
                  <span className="text-ink-3 text-xs">{t("trainerEndTime")}</span>
                  <input
                    type="time"
                    value={day.endTime}
                    onChange={(e) =>
                      updateDay(day.date, { endTime: e.target.value })
                    }
                    className="border border-rule rounded-lg px-2.5 py-1.5 text-sm bg-white focus:outline-none focus:border-tennis-green"
                  />
                </div>

                {/* Trainers present */}
                <p className="text-[10.5px] uppercase tracking-[0.04em] text-ink-3 mb-1.5">
                  {t("dayTrainers")}
                </p>
                <div className="space-y-2">
                  {day.trainers.map((tr) => (
                    <div
                      key={tr.trainerId}
                      className="flex items-center gap-2.5"
                    >
                      <span className="flex-1 text-[13px] font-semibold text-ink">
                        {trainerName(tr.trainerId)}
                      </span>
                      <input
                        type="time"
                        value={tr.startTime}
                        min={day.startTime}
                        max={day.endTime}
                        onChange={(e) =>
                          updateTrainerTime(
                            day.date,
                            tr.trainerId,
                            "startTime",
                            e.target.value,
                          )
                        }
                        className="border border-rule rounded-lg px-2 py-1 text-sm bg-white focus:outline-none focus:border-tennis-green"
                      />
                      <span className="text-ink-3 text-xs">-</span>
                      <input
                        type="time"
                        value={tr.endTime}
                        min={day.startTime}
                        max={day.endTime}
                        onChange={(e) =>
                          updateTrainerTime(
                            day.date,
                            tr.trainerId,
                            "endTime",
                            e.target.value,
                          )
                        }
                        className="border border-rule rounded-lg px-2 py-1 text-sm bg-white focus:outline-none focus:border-tennis-green"
                      />
                      <button
                        type="button"
                        onClick={() => removeTrainer(day.date, tr.trainerId)}
                        aria-label={t("removeTrainer")}
                        className="w-7 h-7 flex items-center justify-center rounded-lg text-ink-3 hover:text-red-500 hover:bg-red-50 transition-colors"
                      >
                        <Trash2 size={13} />
                      </button>
                    </div>
                  ))}
                </div>

                {/* Add trainer */}
                {available.length > 0 ? (
                  <div className="mt-3 flex items-center gap-2">
                    <Plus size={12} className="text-tennis-green" />
                    <NativeSelect
                      value=""
                      onChange={(e) => addTrainer(day.date, e.target.value)}
                    >
                      <option value="">{t("addTrainer")}</option>
                      {available.map((tr) => (
                        <option key={tr.id} value={tr.id}>
                          {tr.firstName} {tr.lastName}
                        </option>
                      ))}
                    </NativeSelect>
                  </div>
                ) : (
                  assignableTrainers.length === 0 && (
                    <p className={hintCls + " mt-3"}>{t("noTrainers")}</p>
                  )
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* ── Submit ── */}
      {errorMessages && errorMessages.length > 0 && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-sm text-red-600 space-y-1">
          {errorMessages.map((msg, i) => (
            <p key={i}>{msg}</p>
          ))}
        </div>
      )}

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={submitting}
          className="inline-flex items-center gap-1.5 px-5 py-2.5 bg-ink text-white text-sm font-semibold rounded-lg disabled:opacity-60"
        >
          {submitting ? t("saving") : t("save")}
        </button>
      </div>
    </form>
  );
}
