"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { ChevronRight, CreditCard } from "lucide-react";

/** Ingang naar Instellingen > Abonnement (enkel admins zien de instellingen). */
export function SubscriptionLinkCard() {
  const t = useTranslations("billing");

  return (
    <Link
      href="/dashboard/settings/subscription"
      className="mb-6 flex items-center gap-3 bg-white rounded-xl shadow-sm shadow-gray-100 px-6 py-4 hover:shadow-md transition-shadow"
    >
      <div className="w-7 h-7 rounded-lg bg-tennis-green/10 flex items-center justify-center">
        <CreditCard size={14} className="text-tennis-green" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-semibold text-gray-800">{t("settingsCardTitle")}</p>
        <p className="text-xs text-gray-400">{t("settingsCardBody")}</p>
      </div>
      <span className="flex items-center gap-1 text-xs font-semibold text-tennis-green">
        {t("settingsCardCta")}
        <ChevronRight size={14} />
      </span>
    </Link>
  );
}
