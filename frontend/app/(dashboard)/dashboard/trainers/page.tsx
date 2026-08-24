"use client";

import { useEffect, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { useTranslations } from "next-intl";
import {
  GraduationCap,
  UserX,
  Trash2,
  Mail,
  X,
  Pencil,
  StickyNote,
  CalendarClock,
  Crown,
} from "lucide-react";
import { TrainerAvailabilityDialog } from "./_components/trainer-availability-dialog";
import { Mono } from "@/components/ui/mono";
import {
  getTrainers,
  inviteTrainer,
  updateTrainer,
  deactivateTrainer,
  reassignTrainerSeries,
  removeTrainer,
  resendTrainerInvite,
  setHeadTrainer,
  TrainerDto,
} from "@/lib/api/trainers";
import { Textarea } from "@/components/ui/textarea";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { getInitials } from "@/lib/utils/initials";
import { getAuthUser, type AuthUser } from "@/lib/auth";
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
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { FieldError } from "@/components/forms/field-error";
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";

// ─── Schema ───────────────────────────────────────────────────────────────────

const inviteSchema = z.object({
  firstName: z.string().min(1, "Voornaam is verplicht").max(100),
  lastName: z.string().min(1, "Achternaam is verplicht").max(100),
  email: z.email("Ongeldig e-mailadres"),
});

type InviteFormValues = z.infer<typeof inviteSchema>;

const editSchema = z.object({
  firstName: z.string().min(1, "Voornaam is verplicht").max(100),
  lastName: z.string().min(1, "Achternaam is verplicht").max(100),
  weeklyCapacityHours: z
    .number({ message: "Vul een aantal uur in" })
    .int()
    .min(1, "Minimaal 1 uur")
    .max(80, "Maximaal 80 uur"),
  notes: z.string().max(1000, "Maximaal 1000 karakters").optional(),
});

type EditFormValues = z.infer<typeof editSchema>;

// ─── Capacity label helper ────────────────────────────────────────────────────

function capacityLabel(
  booked: number,
  capacity: number,
  t: ReturnType<typeof useTranslations>
): { text: string; color: string } {
  if (capacity <= 0) return { text: "", color: "" };
  const ratio = booked / capacity;
  if (ratio >= 0.85) return { text: t("loadAlmostFull"), color: "text-red-600" };
  if (ratio >= 0.5) return { text: t("loadHealthy"), color: "text-tennis-green" };
  return { text: t("loadSpaceAvailable"), color: "text-blue-600" };
}

// ─── Remove dialogs ────────────────────────────────────────────────────────────

function RemoveConfirmDialog({
  trainer,
  onClose,
  onConfirm,
  isRemoving,
}: {
  trainer: TrainerDto;
  onClose: () => void;
  onConfirm: () => void;
  isRemoving: boolean;
}) {
  const t = useTranslations("trainers");
  const tCommon = useTranslations("common");

  return (
    <AlertDialog open onOpenChange={(open) => !open && onClose()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("removeTitle")}</AlertDialogTitle>
          <AlertDialogDescription>
            {t("removeConfirm", { name: `${trainer.firstName} ${trainer.lastName}` })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={onClose}>{tCommon("cancel")}</AlertDialogCancel>
          <AlertDialogAction
            onClick={onConfirm}
            disabled={isRemoving}
            className="bg-red-600 hover:bg-red-700 focus:ring-red-600"
          >
            {isRemoving ? t("removing") : tCommon("delete")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ReassignAndRemoveDialog({
  trainer,
  activeTrainers,
  onClose,
  onConfirm,
  isProcessing,
}: {
  trainer: TrainerDto;
  activeTrainers: TrainerDto[];
  onClose: () => void;
  onConfirm: (toTrainerId: string) => void;
  isProcessing: boolean;
}) {
  const [selectedId, setSelectedId] = useState("");
  const t = useTranslations("trainers");
  const tCommon = useTranslations("common");

  const candidates = activeTrainers.filter((at) => at.id !== trainer.id);

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("removeTitle")}</DialogTitle>
        </DialogHeader>

        <p className="text-sm text-gray-600">
          {t("removeHasSeries", {
            name: `${trainer.firstName} ${trainer.lastName}`,
            count: trainer.lessonSeriesCount,
          })}
        </p>

        <div className="mt-2">
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            {t("reassignTo")}
          </label>
          <NativeSelect
            value={selectedId}
            onChange={(e) => setSelectedId(e.target.value)}
            className="w-full"
          >
            <option value="">{t("selectTrainer")}</option>
            {candidates.map((c) => (
              <option key={c.id} value={c.id}>
                {c.firstName} {c.lastName}
              </option>
            ))}
          </NativeSelect>
          {candidates.length === 0 && (
            <p className="text-xs text-red-500 mt-1">
              {t("noActiveTrainers")}
            </p>
          )}
        </div>

        <DialogFooter>
          <button
            onClick={onClose}
            className="px-4 py-2 border border-gray-200 text-sm font-medium text-gray-600 rounded-lg hover:bg-gray-50 transition-colors"
          >
            {tCommon("cancel")}
          </button>
          <button
            onClick={() => onConfirm(selectedId)}
            disabled={!selectedId || isProcessing || candidates.length === 0}
            className="px-4 py-2 bg-red-600 text-white text-sm font-semibold rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isProcessing ? t("processing") : t("reassignAndRemove")}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Invite Form ──────────────────────────────────────────────────────────────

function InviteForm({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [successEmail, setSuccessEmail] = useState<string | null>(null);
  const [apiErrors, setApiErrors] = useState<string[]>([]);
  const t = useTranslations("trainers");
  const tCommon = useTranslations("common");

  const mutation = useMutation({
    mutationFn: inviteTrainer,
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
      setSuccessEmail(vars.email);
    },
    onError: (err) => {
      setApiErrors(
        getAxiosErrorMessages(err, "Er ging iets mis. Probeer het opnieuw.")
      );
    },
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<InviteFormValues>({ resolver: zodResolver(inviteSchema) });

  if (successEmail) {
    return (
      <div className="text-center py-6">
        <div className="w-12 h-12 rounded-full bg-green-50 flex items-center justify-center mx-auto mb-4">
          <Mail size={22} className="text-green-600" />
        </div>
        <h3 className="text-base font-semibold text-gray-900 mb-1">{t("inviteSent")}</h3>
        <p className="text-sm text-gray-400 mb-6">
          {t("inviteSentDesc")}
          <br />
          <span className="font-medium text-gray-600">{successEmail}</span>
        </p>
        <button
          onClick={onClose}
          className="px-4 py-2 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
        >
          {t("inviteClose")}
        </button>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit((d) => {
        setApiErrors([]);
        mutation.mutate(d);
      })}
      className="space-y-4"
    >
      {apiErrors.length > 0 && (
        <div className="bg-red-50 border border-red-100 rounded-lg px-4 py-3 text-sm text-red-600">
          {apiErrors.map((e, i) => <p key={i}>{e}</p>)}
        </div>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            {t("firstName")} <span className="text-red-400">*</span>
          </label>
          <input
            {...register("firstName")}
            type="text"
            placeholder="Jan"
            className={inputClass}
          />
          <FieldError message={errors.firstName?.message} />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            {t("lastName")} <span className="text-red-400">*</span>
          </label>
          <input
            {...register("lastName")}
            type="text"
            placeholder="Janssen"
            className={inputClass}
          />
          <FieldError message={errors.lastName?.message} />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">
          {t("email")} <span className="text-red-400">*</span>
        </label>
        <input
          {...register("email")}
          type="email"
          placeholder="jan@tennisclub.be"
          className={inputClass}
        />
        <FieldError message={errors.email?.message} />
      </div>

      <div className="flex items-center gap-3 pt-1">
        <button
          type="button"
          onClick={onClose}
          className="flex-1 px-4 py-2.5 border border-gray-200 text-sm font-medium text-gray-600 rounded-lg hover:bg-gray-50 transition-colors"
        >
          {tCommon("cancel")}
        </button>
        <button
          type="submit"
          disabled={mutation.isPending}
          className="flex-1 px-4 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {mutation.isPending ? t("inviteSending") : t("inviteSend")}
        </button>
      </div>
    </form>
  );
}

// ─── Edit dialog ───────────────────────────────────────────────────────────────

function EditTrainerDialog({
  trainer,
  onClose,
}: {
  trainer: TrainerDto;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [apiErrors, setApiErrors] = useState<string[]>([]);
  const t = useTranslations("trainers");
  const tCommon = useTranslations("common");

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      firstName: trainer.firstName,
      lastName: trainer.lastName,
      weeklyCapacityHours: trainer.weeklyCapacityHours,
      notes: trainer.notes ?? "",
    },
  });

  const mutation = useMutation({
    mutationFn: (values: EditFormValues) =>
      updateTrainer(trainer.id, {
        firstName: values.firstName,
        lastName: values.lastName,
        weeklyCapacityHours: values.weeklyCapacityHours,
        notes: values.notes && values.notes.trim() ? values.notes : null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
      onClose();
    },
    onError: (err) => {
      setApiErrors(getAxiosErrorMessages(err, t("editError")));
    },
  });

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("editTitle")}</DialogTitle>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((d) => {
            setApiErrors([]);
            mutation.mutate(d);
          })}
          className="space-y-4"
        >
          {apiErrors.length > 0 && (
            <div className="bg-red-50 border border-red-100 rounded-lg px-4 py-3 text-sm text-red-600">
              {apiErrors.map((e, i) => <p key={i}>{e}</p>)}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("firstName")} <span className="text-red-400">*</span>
              </label>
              <input {...register("firstName")} type="text" className={inputClass} />
              <FieldError message={errors.firstName?.message} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("lastName")} <span className="text-red-400">*</span>
              </label>
              <input {...register("lastName")} type="text" className={inputClass} />
              <FieldError message={errors.lastName?.message} />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              {t("weeklyCapacity")} <span className="text-red-400">*</span>
            </label>
            <input
              {...register("weeklyCapacityHours", { valueAsNumber: true })}
              type="number"
              min={1}
              max={80}
              step={1}
              className={inputClass}
            />
            <p className="text-[11px] text-gray-400 mt-1">{t("weeklyCapacityHint")}</p>
            <FieldError message={errors.weeklyCapacityHours?.message} />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              {t("notes")}
            </label>
            <Textarea
              {...register("notes")}
              rows={3}
              placeholder={t("notesPlaceholder")}
            />
            <FieldError message={errors.notes?.message} />
          </div>

          <div className="flex items-center gap-3 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2.5 border border-gray-200 text-sm font-medium text-gray-600 rounded-lg hover:bg-gray-50 transition-colors"
            >
              {tCommon("cancel")}
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="flex-1 px-4 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {mutation.isPending ? t("saving") : t("save")}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

type RemoveDialogState =
  | { type: "none" }
  | { type: "confirm"; trainer: TrainerDto }
  | { type: "reassign"; trainer: TrainerDto };

export default function TrainersPage() {
  const [showInviteForm, setShowInviteForm] = useState(false);
  const [removeDialog, setRemoveDialog] = useState<RemoveDialogState>({ type: "none" });
  const [editTrainer, setEditTrainer] = useState<TrainerDto | null>(null);
  const [availabilityTrainer, setAvailabilityTrainer] = useState<TrainerDto | null>(null);
  const [user, setUser] = useState<AuthUser | null>(null);
  const queryClient = useQueryClient();
  const t = useTranslations("trainers");

  useEffect(() => {
    setUser(getAuthUser());
  }, []);

  const { data: trainers, isLoading } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateTrainer,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trainers"] }),
  });

  const headTrainerMutation = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) =>
      setHeadTrainer(id, value),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trainers"] }),
  });

  const removeMutation = useMutation({
    mutationFn: removeTrainer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
      setRemoveDialog({ type: "none" });
    },
  });

  const reassignAndRemoveMutation = useMutation({
    mutationFn: async ({ fromId, toId }: { fromId: string; toId: string }) => {
      await reassignTrainerSeries(fromId, toId);
      await removeTrainer(fromId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
      setRemoveDialog({ type: "none" });
    },
  });

  const resendMutation = useMutation({
    mutationFn: resendTrainerInvite,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trainers"] }),
  });

  function handleRemoveClick(trainer: TrainerDto) {
    if (trainer.lessonSeriesCount > 0) {
      setRemoveDialog({ type: "reassign", trainer });
    } else {
      setRemoveDialog({ type: "confirm", trainer });
    }
  }

  const active = trainers?.filter((tr) => tr.isActive) ?? [];
  const invited = trainers?.filter((tr) => !tr.isActive && tr.invitePending) ?? [];
  const deactivated = trainers?.filter((tr) => !tr.isActive && !tr.invitePending) ?? [];
  const allTrainers = [...active, ...invited, ...deactivated];
  const activeCount = active.length;
  const invitedCount = invited.length;

  return (
    <>
      {/* Header */}
      <div className="flex items-center justify-between mb-5">
        <div>
          <p className="font-mono text-[10.5px] text-ink-3 uppercase tracking-[0.1em] m-0">
            {activeCount} {t("active")} · {invitedCount} {t("invited")}
          </p>
          <h1 className="text-lg font-bold text-ink tracking-tight mt-0.5">
            {t("title")}
          </h1>
        </div>
        {!showInviteForm && (
          <button
            onClick={() => setShowInviteForm(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-ink text-white text-[11.5px] font-semibold rounded-md cursor-pointer"
          >
            + {t("invite")}
          </button>
        )}
      </div>

      {/* Invite form panel */}
      {showInviteForm && (
        <div className="bg-paper border border-rule rounded-xl p-6 mb-5 relative">
          <div className="flex items-center justify-between mb-5">
            <h2 className="text-base font-semibold text-ink">{t("inviteTitle")}</h2>
            <button
              onClick={() => setShowInviteForm(false)}
              className="p-1.5 rounded-lg text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
            >
              <X size={16} />
            </button>
          </div>
          <InviteForm onClose={() => setShowInviteForm(false)} />
        </div>
      )}

      {/* Trainer card grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-3.5">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="bg-paper border border-rule rounded-xl p-4 animate-pulse h-[160px]" />
          ))}
        </div>
      ) : !trainers?.length ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <div className="w-14 h-14 rounded-full bg-tennis-green/[.08] flex items-center justify-center mb-4">
            <GraduationCap size={26} className="text-tennis-green/40" />
          </div>
          <p className="text-ink font-semibold mb-1">{t("noTrainers")}</p>
          <p className="text-ink-3 text-sm max-w-56 leading-relaxed">
            {t("noTrainersDesc")}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-3.5">
          {allTrainers.map((tr) => {
            const initials = getInitials(tr.firstName, tr.lastName);
            const isInvited = !tr.isActive && tr.invitePending;
            const isDeactivatedState = !tr.isActive && !tr.invitePending;
            // De eigen admin verschijnt alleen in deze lijst zolang AdminsActAsTrainers
            // aanstaat in /dashboard/settings. Beheer gebeurt daar, niet via deactivate/remove
            // (de Admin-rol kun je op de trainerlijst niet wijzigen).
            const isSelfAdmin = user?.role === "Admin" && tr.id === user?.userId;
            const cap = capacityLabel(tr.currentWeekHoursBooked, tr.weeklyCapacityHours, t);
            const loadPct = tr.weeklyCapacityHours > 0
              ? Math.min(100, Math.round((tr.currentWeekHoursBooked / tr.weeklyCapacityHours) * 100))
              : 0;

            return (
              <div key={tr.id} className="bg-paper border border-rule rounded-xl p-4 group">
                <div className="flex items-start gap-3">
                  <div className="w-10 h-10 rounded-[10px] bg-tennis-green grid place-items-center shrink-0">
                    <span className="text-tennis-lime font-bold text-[13px]">{initials}</span>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex justify-between items-center">
                      <p className="text-[13.5px] font-bold text-ink m-0 flex items-center gap-1.5">
                        {tr.firstName} {tr.lastName}
                        {tr.notes && tr.notes.trim() && (
                          <StickyNote
                            size={12}
                            className="text-amber-500 shrink-0"
                            aria-label={t("notesIndicator")}
                          >
                            <title>{tr.notes}</title>
                          </StickyNote>
                        )}
                        {tr.isHeadTrainer && (
                          <span className="shrink-0 inline-flex items-center gap-1 rounded-full bg-tennis-lime/30 px-1.5 py-0.5 text-[9px] font-semibold text-tennis-green">
                            <Crown size={9} />
                            {t("headTrainerBadge")}
                          </span>
                        )}
                      </p>
                      {isSelfAdmin ? (
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-tennis-lime/30 text-tennis-green font-semibold">
                          {t("adminBadge")}
                        </span>
                      ) : isInvited ? (
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 font-semibold">
                          {t("invitedLabel")}
                        </span>
                      ) : isDeactivatedState ? (
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-red-50 text-red-600 font-semibold">
                          {t("inactiveLabel")}
                        </span>
                      ) : (
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-tennis-green/10 text-tennis-green font-semibold">
                          {t("activeLabel")}
                        </span>
                      )}
                    </div>
                    <p className="text-[11px] text-ink-3 font-mono mt-0.5 m-0">{tr.email}</p>
                  </div>
                </div>

                {tr.isActive && (
                  <>
                    <div className="grid grid-cols-3 gap-2.5 mt-3.5 pt-3 border-t border-dashed border-rule">
                      <div>
                        <p className="text-[9.5px] text-ink-3 uppercase tracking-[0.06em] m-0">{t("seriesCount")}</p>
                        <p className="text-sm font-bold text-ink font-mono mt-0.5 m-0">
                          {tr.lessonSeriesCount}
                        </p>
                      </div>
                      <div>
                        <p className="text-[9.5px] text-ink-3 uppercase tracking-[0.06em] m-0">{t("weekLoad")}</p>
                        {tr.weeklyCapacityHours > 0 ? (
                          <div className="mt-1">
                            <div className="flex items-center gap-1.5">
                              <div className="flex-1 h-[5px] bg-gray-100 rounded-full overflow-hidden">
                                <div
                                  className={`h-full rounded-full transition-all ${
                                    loadPct >= 85 ? "bg-red-500" : loadPct >= 50 ? "bg-tennis-green" : "bg-blue-400"
                                  }`}
                                  style={{ width: `${loadPct}%` }}
                                />
                              </div>
                              <Mono className="text-[10px] text-ink-3">{tr.currentWeekHoursBooked}/{tr.weeklyCapacityHours}u</Mono>
                            </div>
                            {cap.text && (
                              <p className={`text-[9px] mt-0.5 m-0 font-medium ${cap.color}`}>
                                {cap.text}
                              </p>
                            )}
                          </div>
                        ) : (
                          <p className="text-sm font-bold text-ink font-mono mt-0.5 m-0">&mdash;</p>
                        )}
                      </div>
                      <div className="flex gap-1 justify-end items-end">
                        {isSelfAdmin ? (
                          <span className="text-[9.5px] text-ink-3 italic">
                            {t("manageViaSettings")}
                          </span>
                        ) : (
                          <>
                            <button
                              onClick={() =>
                                headTrainerMutation.mutate({ id: tr.id, value: !tr.isHeadTrainer })
                              }
                              disabled={headTrainerMutation.isPending}
                              title={tr.isHeadTrainer ? t("removeHeadTrainer") : t("makeHeadTrainer")}
                              aria-label={tr.isHeadTrainer ? t("removeHeadTrainer") : t("makeHeadTrainer")}
                              className={`p-1.5 rounded transition-all disabled:opacity-50 ${
                                tr.isHeadTrainer
                                  ? "text-tennis-green bg-tennis-green/10"
                                  : "opacity-0 group-hover:opacity-100 text-ink-3 hover:text-tennis-green hover:bg-tennis-green/10"
                              }`}
                            >
                              <Crown size={14} />
                            </button>
                            <button
                              onClick={() => setAvailabilityTrainer(tr)}
                              title={t("availabilityButton")}
                              aria-label={t("availabilityButton")}
                              className="opacity-0 group-hover:opacity-100 p-1.5 rounded text-ink-3 hover:text-tennis-green hover:bg-tennis-green/10 transition-all"
                            >
                              <CalendarClock size={14} />
                            </button>
                            <button
                              onClick={() => setEditTrainer(tr)}
                              title={t("edit")}
                              className="opacity-0 group-hover:opacity-100 p-1.5 rounded text-ink-3 hover:text-tennis-green hover:bg-tennis-green/10 transition-all"
                            >
                              <Pencil size={14} />
                            </button>
                            <button
                              onClick={() => deactivateMutation.mutate(tr.id)}
                              disabled={deactivateMutation.isPending}
                              title={t("deactivate")}
                              className="opacity-0 group-hover:opacity-100 p-1.5 rounded text-ink-3 hover:text-amber-500 hover:bg-amber-50 transition-all"
                            >
                              <UserX size={14} />
                            </button>
                            {tr.lessonSeriesCount === 0 && (
                              <button
                                onClick={() => handleRemoveClick(tr)}
                                title={t("remove")}
                                className="opacity-0 group-hover:opacity-100 p-1.5 rounded text-ink-3 hover:text-red-500 hover:bg-red-50 transition-all"
                              >
                                <Trash2 size={14} />
                              </button>
                            )}
                          </>
                        )}
                      </div>
                    </div>
                  </>
                )}

                {isInvited && (
                  <div className="mt-3 px-3 py-2.5 bg-amber-50/60 rounded-lg border border-dashed border-amber-200 flex justify-between items-center">
                    <p className="text-[11px] text-amber-800 m-0">{t("invitePending")}</p>
                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={() => setEditTrainer(tr)}
                        title={t("edit")}
                        className="p-1.5 rounded text-amber-700 hover:text-tennis-green hover:bg-tennis-green/10 transition-all"
                      >
                        <Pencil size={14} />
                      </button>
                      <button
                        onClick={() => resendMutation.mutate(tr.id)}
                        disabled={resendMutation.isPending}
                        className="px-2.5 py-1 bg-white border border-amber-300 rounded text-[10.5px] text-amber-800 font-semibold disabled:opacity-50"
                      >
                        {resendMutation.isPending ? t("resending") : t("resend")}
                      </button>
                      <button
                        onClick={() => handleRemoveClick(tr)}
                        title={t("revokeInvite")}
                        className="p-1.5 rounded text-amber-700 hover:text-red-500 hover:bg-red-50 transition-all"
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </div>
                )}

                {isDeactivatedState && (
                  <div className="mt-3 flex gap-1 justify-end">
                    <button
                      onClick={() => setEditTrainer(tr)}
                      title={t("edit")}
                      className="p-1.5 rounded text-ink-3 hover:text-tennis-green hover:bg-tennis-green/10 transition-all"
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      onClick={() => handleRemoveClick(tr)}
                      title={t("remove")}
                      className="p-1.5 rounded text-ink-3 hover:text-red-500 hover:bg-red-50 transition-all"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Remove dialogs */}
      {removeDialog.type === "confirm" && (
        <RemoveConfirmDialog
          trainer={removeDialog.trainer}
          onClose={() => setRemoveDialog({ type: "none" })}
          onConfirm={() => removeMutation.mutate(removeDialog.trainer.id)}
          isRemoving={removeMutation.isPending}
        />
      )}
      {removeDialog.type === "reassign" && (
        <ReassignAndRemoveDialog
          trainer={removeDialog.trainer}
          activeTrainers={active}
          onClose={() => setRemoveDialog({ type: "none" })}
          onConfirm={(toId) =>
            reassignAndRemoveMutation.mutate({
              fromId: removeDialog.trainer.id,
              toId,
            })
          }
          isProcessing={reassignAndRemoveMutation.isPending}
        />
      )}
      {editTrainer && (
        <EditTrainerDialog
          trainer={editTrainer}
          onClose={() => setEditTrainer(null)}
        />
      )}
      {availabilityTrainer && (
        <TrainerAvailabilityDialog
          trainer={availabilityTrainer}
          onClose={() => setAvailabilityTrainer(null)}
        />
      )}
    </>
  );
}

