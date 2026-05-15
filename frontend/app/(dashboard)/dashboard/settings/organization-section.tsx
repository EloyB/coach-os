"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Settings as SettingsIcon } from "lucide-react";
import {
  getOrganizationSettings,
  updateOrganizationSettings,
} from "@/lib/api/organizationSettings";

export function OrganizationSection() {
  const t = useTranslations("organizationSettings");
  const queryClient = useQueryClient();

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
    },
  });

  function handleToggleAdminsActAsTrainers(next: boolean) {
    updateMutation.mutate({ adminsActAsTrainers: next });
  }

  const isOn = settings?.adminsActAsTrainers ?? false;
  const disabled = isLoading || updateMutation.isPending;

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden mb-6">
      {/* Section header */}
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
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
            onClick={() => handleToggleAdminsActAsTrainers(!isOn)}
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
  );
}
