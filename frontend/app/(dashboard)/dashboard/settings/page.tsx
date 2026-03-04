"use client";

import Link from "next/link";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Building2, Trash2, Plus, MapPin } from "lucide-react";
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
  getTennisClubs,
  createTennisClub,
  deleteTennisClub,
  TennisClubDto,
} from "@/lib/api/tennisClubs";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";

// ─── Form Schema ──────────────────────────────────────────────────────────────

const addClubSchema = z.object({
  name: z.string().min(1, "Naam is verplicht").max(200),
  address: z.string().min(1, "Adres is verplicht").max(500),
});

type AddClubFormValues = z.infer<typeof addClubSchema>;

// ─── Club Row ─────────────────────────────────────────────────────────────────

function ClubRow({ club }: { club: TennisClubDto }) {
  const t = useTranslations("tennisClubs");
  const queryClient = useQueryClient();
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const deleteMutation = useMutation({
    mutationFn: () => deleteTennisClub(club.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tennisClubs"] });
      setDeleteError(null);
    },
    onError: () => {
      setDeleteError(t("inUse"));
    },
  });

  return (
    <div className="flex items-start justify-between gap-4 p-4 rounded-lg border border-gray-100 bg-white hover:border-gray-200 transition-colors">
      <div className="flex items-start gap-3 flex-1 min-w-0">
        <div className="w-8 h-8 rounded-lg bg-tennis-green/8 flex items-center justify-center shrink-0 mt-0.5">
          <Building2 size={14} className="text-tennis-green" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-gray-800 leading-tight">
            {club.name}
          </p>
          <div className="flex items-center gap-1 mt-0.5">
            <MapPin size={11} className="text-gray-400 shrink-0" />
            <p className="text-xs text-gray-500 truncate">{club.address}</p>
          </div>
          {deleteError && (
            <p className="text-xs text-amber-600 mt-1.5 leading-snug">
              {deleteError}
            </p>
          )}
        </div>
      </div>

      <AlertDialog>
        <AlertDialogTrigger asChild>
          <button className="shrink-0 w-7 h-7 flex items-center justify-center rounded-lg text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors mt-0.5">
            <Trash2 size={13} />
          </button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("confirmDelete")}</AlertDialogTitle>
            <AlertDialogDescription>
              <strong>&ldquo;{club.name}&rdquo;</strong> wordt permanent
              verwijderd. Dit kan niet ongedaan worden gemaakt.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annuleren</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteMutation.mutate()}
              disabled={deleteMutation.isPending}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              {deleteMutation.isPending ? "Verwijderen..." : t("delete")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function SettingsPage() {
  const t = useTranslations("tennisClubs");
  const queryClient = useQueryClient();

  const { data: clubs, isLoading } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const createMutation = useMutation({
    mutationFn: createTennisClub,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tennisClubs"] });
      reset();
    },
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AddClubFormValues>({
    resolver: zodResolver(addClubSchema),
  });

  function onSubmit(data: AddClubFormValues) {
    createMutation.mutate({ name: data.name, address: data.address });
  }

  return (
    <div className="max-w-2xl mx-auto">
      {/* Page title */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          Instellingen
        </h1>
        <p className="text-gray-400 text-sm mt-1">
          Beheer de basisinstellingen van je organisatie.
        </p>
      </div>

      {/* Tennisclubs section */}
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
        {/* Section header */}
        <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
          <div className="w-7 h-7 rounded-lg bg-tennis-green/10 flex items-center justify-center">
            <Building2 size={14} className="text-tennis-green" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-gray-800">{t("title")}</h2>
            <p className="text-xs text-gray-400">
              Locaties waar lessen worden gegeven
            </p>
          </div>
        </div>

        {/* Club list */}
        <div className="p-5 space-y-2">
          {isLoading && (
            <div className="space-y-2 animate-pulse">
              {[1, 2].map((i) => (
                <div key={i} className="h-14 bg-gray-100 rounded-lg" />
              ))}
            </div>
          )}

          {!isLoading && clubs?.length === 0 && (
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <div className="w-10 h-10 rounded-xl bg-gray-100 flex items-center justify-center mb-3">
                <Building2 size={18} className="text-gray-300" />
              </div>
              <p className="text-sm text-gray-400 font-medium">{t("empty")}</p>
              <p className="text-xs text-gray-300 mt-1">
                Voeg hieronder een club toe.
              </p>
            </div>
          )}

          {!isLoading &&
            clubs?.map((club) => <ClubRow key={club.id} club={club} />)}
        </div>

        {/* Divider */}
        <div className="mx-5 border-t border-gray-100" />

        {/* Add form */}
        <form onSubmit={handleSubmit(onSubmit)} className="p-5 space-y-3">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
            Nieuwe club toevoegen
          </p>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                {t("name")} <span className="text-red-400">*</span>
              </label>
              <input
                {...register("name")}
                type="text"
                placeholder="TC Brederode"
                className={inputClass}
              />
              <FieldError message={errors.name?.message} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                {t("address")} <span className="text-red-400">*</span>
              </label>
              <input
                {...register("address")}
                type="text"
                placeholder="Brederodestraat 5, Haarlem"
                className={inputClass}
              />
              <FieldError message={errors.address?.message} />
            </div>
          </div>

          {createMutation.isError && (
            <p className="text-xs text-red-500">
              Er ging iets mis. Probeer opnieuw.
            </p>
          )}

          <button
            type="submit"
            disabled={createMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {createMutation.isPending ? (
              <span className="w-3.5 h-3.5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
            ) : (
              <Plus size={13} />
            )}
            {createMutation.isPending ? "Toevoegen..." : t("add")}
          </button>
        </form>
      </div>
    </div>
  );
}
