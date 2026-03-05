"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import axios from "axios";
import {
  GraduationCap,
  Plus,
  UserX,
  Mail,
  CheckCircle2,
  Clock,
  X,
} from "lucide-react";
import {
  getTrainers,
  inviteTrainer,
  deactivateTrainer,
  TrainerDto,
} from "@/lib/api/trainers";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";

// ─── Schema ───────────────────────────────────────────────────────────────────

const inviteSchema = z.object({
  firstName: z.string().min(1, "Voornaam is verplicht").max(100),
  lastName: z.string().min(1, "Achternaam is verplicht").max(100),
  email: z.email("Ongeldig e-mailadres"),
});

type InviteFormValues = z.infer<typeof inviteSchema>;

// ─── Skeleton ─────────────────────────────────────────────────────────────────

function SkeletonRow() {
  return (
    <div className="flex items-center justify-between py-4 border-b border-gray-100 last:border-0 animate-pulse">
      <div className="flex items-center gap-3">
        <div className="w-9 h-9 rounded-full bg-gray-200" />
        <div>
          <div className="h-4 bg-gray-200 rounded w-32 mb-1.5" />
          <div className="h-3 bg-gray-100 rounded w-44" />
        </div>
      </div>
      <div className="h-6 w-20 bg-gray-100 rounded-full" />
    </div>
  );
}

// ─── Trainer Row ──────────────────────────────────────────────────────────────

function TrainerRow({
  trainer,
  onDeactivate,
  isDeactivating,
}: {
  trainer: TrainerDto;
  onDeactivate: (id: string) => void;
  isDeactivating: boolean;
}) {
  const initials = `${trainer.firstName[0] ?? ""}${trainer.lastName[0] ?? ""}`.toUpperCase();

  return (
    <div className="flex items-center justify-between py-4 border-b border-gray-100 last:border-0 group">
      <div className="flex items-center gap-3 min-w-0">
        {/* Avatar */}
        <div className="w-9 h-9 rounded-full bg-tennis-green/10 flex items-center justify-center shrink-0">
          <span className="text-tennis-green text-sm font-semibold">{initials}</span>
        </div>
        <div className="min-w-0">
          <p className="text-sm font-medium text-gray-900 truncate">
            {trainer.firstName} {trainer.lastName}
          </p>
          <p className="text-xs text-gray-400 truncate">{trainer.email}</p>
        </div>
      </div>

      <div className="flex items-center gap-3 shrink-0 ml-4">
        {/* Status badge */}
        {trainer.isActive ? (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-green-50 text-green-700">
            <CheckCircle2 size={11} />
            Actief
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-700">
            <Clock size={11} />
            Uitgenodigd
          </span>
        )}

        {/* Deactivate */}
        {trainer.isActive && (
          <button
            onClick={() => onDeactivate(trainer.id)}
            disabled={isDeactivating}
            title="Deactiveer trainer"
            className="opacity-0 group-hover:opacity-100 p-1.5 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 transition-all disabled:opacity-40"
          >
            <UserX size={15} />
          </button>
        )}
      </div>
    </div>
  );
}

// ─── Invite Form ──────────────────────────────────────────────────────────────

function InviteForm({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [successEmail, setSuccessEmail] = useState<string | null>(null);
  const [apiErrors, setApiErrors] = useState<string[]>([]);

  const mutation = useMutation({
    mutationFn: inviteTrainer,
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
      setSuccessEmail(vars.email);
    },
    onError: (err) => {
      if (axios.isAxiosError(err) && err.response?.data) {
        const d = err.response.data;
        setApiErrors(Array.isArray(d) ? d : [String(d)]);
      } else {
        setApiErrors(["Er ging iets mis. Probeer het opnieuw."]);
      }
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
        <h3 className="text-base font-semibold text-gray-900 mb-1">Uitnodiging verzonden</h3>
        <p className="text-sm text-gray-400 mb-6">
          De uitnodigingslink is gelogd in de console (dev mode).
          <br />
          <span className="font-medium text-gray-600">{successEmail}</span>
        </p>
        <button
          onClick={onClose}
          className="px-4 py-2 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
        >
          Sluiten
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
            Voornaam <span className="text-red-400">*</span>
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
            Achternaam <span className="text-red-400">*</span>
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
          E-mailadres <span className="text-red-400">*</span>
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
          Annuleren
        </button>
        <button
          type="submit"
          disabled={mutation.isPending}
          className="flex-1 px-4 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {mutation.isPending ? "Versturen..." : "Uitnodiging sturen"}
        </button>
      </div>
    </form>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function TrainersPage() {
  const [showInviteForm, setShowInviteForm] = useState(false);
  const queryClient = useQueryClient();

  const { data: trainers, isLoading } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateTrainer,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["trainers"] }),
  });

  const active = trainers?.filter((t) => t.isActive) ?? [];
  const invited = trainers?.filter((t) => !t.isActive) ?? [];

  return (
    <div className="max-w-2xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Trainers</h1>
          <p className="text-gray-400 text-sm mt-1">
            Beheer de trainers van je organisatie.
          </p>
        </div>

        {!showInviteForm && (
          <button
            onClick={() => setShowInviteForm(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
          >
            <Plus size={15} />
            Trainer uitnodigen
          </button>
        )}
      </div>

      {/* Invite form panel */}
      {showInviteForm && (
        <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6 mb-6 relative">
          <div className="flex items-center justify-between mb-5">
            <h2 className="text-base font-semibold text-gray-900">Trainer uitnodigen</h2>
            <button
              onClick={() => setShowInviteForm(false)}
              className="p-1.5 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
            >
              <X size={16} />
            </button>
          </div>
          <InviteForm onClose={() => setShowInviteForm(false)} />
        </div>
      )}

      {/* Trainer list */}
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6">
        {isLoading ? (
          <div>
            <SkeletonRow />
            <SkeletonRow />
            <SkeletonRow />
          </div>
        ) : !trainers?.length ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <div className="w-14 h-14 rounded-full bg-tennis-green/8 flex items-center justify-center mb-4">
              <GraduationCap size={26} className="text-tennis-green/40" />
            </div>
            <p className="text-gray-700 font-semibold mb-1">Nog geen trainers</p>
            <p className="text-gray-400 text-sm max-w-56 leading-relaxed">
              Nodig je eerste trainer uit via de knop hierboven.
            </p>
          </div>
        ) : (
          <>
            {active.length > 0 && (
              <div>
                <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">
                  Actief ({active.length})
                </p>
                {active.map((t) => (
                  <TrainerRow
                    key={t.id}
                    trainer={t}
                    onDeactivate={(id) => deactivateMutation.mutate(id)}
                    isDeactivating={deactivateMutation.isPending}
                  />
                ))}
              </div>
            )}
            {invited.length > 0 && (
              <div className={active.length > 0 ? "mt-6" : ""}>
                <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">
                  Uitgenodigd ({invited.length})
                </p>
                {invited.map((t) => (
                  <TrainerRow
                    key={t.id}
                    trainer={t}
                    onDeactivate={(id) => deactivateMutation.mutate(id)}
                    isDeactivating={deactivateMutation.isPending}
                  />
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
