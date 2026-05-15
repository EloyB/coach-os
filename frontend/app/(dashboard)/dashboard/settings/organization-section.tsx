"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Check, Settings as SettingsIcon } from "lucide-react";
import {
  getOrganizationSettings,
  updateOrganizationSettings,
} from "@/lib/api/organizationSettings";
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

const SAVED_HINT_MS = 1800;

export function OrganizationSection() {
  const t = useTranslations("organizationSettings");
  const queryClient = useQueryClient();
  const [showOffConfirm, setShowOffConfirm] = useState(false);
  const [savedAt, setSavedAt] = useState<number | null>(null);

  const { data: settings, isLoading } = useQuery({
    queryKey: ["organizationSettings"],
    queryFn: getOrganizationSettings,
  });

  const updateMutation = useMutation({
    mutationFn: updateOrganizationSettings,
    onSuccess: (data) => {
      queryClient.setQueryData(["organizationSettings"], data);
      // Trainerlijst hangt af van deze setting — verver hem.
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
      setSavedAt(Date.now());
    },
  });

  // Verstop de "opgeslagen"-hint automatisch na SAVED_HINT_MS.
  useEffect(() => {
    if (savedAt === null) return;
    const id = setTimeout(() => setSavedAt(null), SAVED_HINT_MS);
    return () => clearTimeout(id);
  }, [savedAt]);

  function commit(next: boolean) {
    updateMutation.mutate({ adminsActAsTrainers: next });
  }

  function handleClick() {
    if (!settings) return;
    const next = !settings.adminsActAsTrainers;
    // Toggle-off met openstaande lessen → eerst bevestigen.
    if (!next && settings.currentUserUpcomingLessonsAsTrainer > 0) {
      setShowOffConfirm(true);
      return;
    }
    commit(next);
  }

  const isOn = settings?.adminsActAsTrainers ?? false;
  const disabled = isLoading || updateMutation.isPending;
  const upcoming = settings?.currentUserUpcomingLessonsAsTrainer ?? 0;
  const showSaved = savedAt !== null;

  return (
    <>
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden mb-6">
        {/* Section header */}
        <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between gap-2.5">
          <div className="flex items-center gap-2.5">
            <div className="w-7 h-7 rounded-lg bg-tennis-green/10 flex items-center justify-center">
              <SettingsIcon size={14} className="text-tennis-green" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-gray-800">
                {t("sectionTitle")}
              </h2>
              <p className="text-xs text-gray-400">{t("sectionSubtitle")}</p>
            </div>
          </div>
          {showSaved && (
            <span className="flex items-center gap-1 text-[11px] font-semibold text-tennis-green animate-in fade-in slide-in-from-right-1">
              <Check size={12} />
              {t("saved")}
            </span>
          )}
        </div>

        {/* Toggle row */}
        <div className="p-5">
          <label className="flex items-start justify-between gap-4 cursor-pointer">
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-gray-800">
                {t("adminsActAsTrainersLabel")}
              </p>
              <p className="text-xs text-gray-500 mt-1 leading-relaxed">
                {t("adminsActAsTrainersHelp")}
              </p>
            </div>
            <button
              type="button"
              role="switch"
              aria-checked={isOn}
              disabled={disabled}
              onClick={handleClick}
              className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors mt-0.5 disabled:opacity-50 disabled:cursor-not-allowed ${
                isOn ? "bg-tennis-green" : "bg-gray-200"
              }`}
            >
              <span
                className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                  isOn ? "translate-x-6" : "translate-x-1"
                }`}
              />
            </button>
          </label>

          {updateMutation.isError && (
            <p className="text-xs text-red-500 mt-3">{t("saveError")}</p>
          )}
        </div>
      </div>

      <AlertDialog open={showOffConfirm} onOpenChange={setShowOffConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("toggleOffConfirmTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("toggleOffConfirmBody", { count: upcoming })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("toggleOffCancel")}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setShowOffConfirm(false);
                commit(false);
              }}
              className="bg-tennis-green hover:bg-tennis-green/90 text-white"
            >
              {t("toggleOffConfirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
