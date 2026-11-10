"use client";

import { useMutation } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Download } from "lucide-react";

import { Button } from "@/components/ui/button";
import { CLUB_EXPORT_FILENAME, downloadClubExport } from "@/lib/api/privacy";
import { downloadBlob } from "@/lib/download";

/** Download van alle clubgegevens als ZIP (enkel admins zien de instellingen). */
export function ClubExportCard() {
  const t = useTranslations("privacy");
  const exportMutation = useMutation({
    mutationFn: downloadClubExport,
    onSuccess: (blob) => downloadBlob(blob, CLUB_EXPORT_FILENAME),
  });

  return (
    <section className="mb-6 bg-white rounded-xl shadow-sm shadow-gray-100 px-6 py-4">
      <div className="flex items-start gap-3">
        <div className="w-7 h-7 shrink-0 rounded-lg bg-tennis-green/10 flex items-center justify-center">
          <Download size={14} className="text-tennis-green" />
        </div>
        <div className="flex-1 min-w-0">
          <h2 className="text-sm font-semibold text-gray-800">{t("exportTitle")}</h2>
          <p className="text-xs text-gray-400">{t("exportBody")}</p>
          <p className="mt-2 text-xs text-gray-500">{t("exportPrivacy")}</p>
          {exportMutation.isError && (
            <p role="alert" className="mt-2 text-xs text-red-600">
              {t("exportError")}
            </p>
          )}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => exportMutation.mutate()}
          disabled={exportMutation.isPending}
        >
          {exportMutation.isPending ? t("exportBusy") : t("exportCta")}
        </Button>
      </div>
    </section>
  );
}
