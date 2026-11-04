"use client";

import { AlertTriangle } from "lucide-react";
import { useTranslations } from "next-intl";

export function FormPrivacyWarning() {
  const t = useTranslations("formBuilder");

  return (
    <div role="note" className="flex gap-3 rounded-lg border border-warn/30 bg-warn/5 p-4 text-sm text-ink">
      <AlertTriangle size={18} aria-hidden="true" className="mt-0.5 shrink-0 text-warn" />
      <div className="min-w-0 space-y-1">
        <p className="font-semibold">{t("privacyWarningTitle")}</p>
        <p className="text-ink-2">{t("privacyWarningDescription")}</p>
      </div>
    </div>
  );
}
