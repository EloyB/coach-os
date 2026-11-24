"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getOrganizationSettings } from "@/lib/api/organizationSettings";
import { getAuthUser } from "@/lib/auth";
import { useClientValue } from "@/lib/hooks/use-client-value";
import { AvailabilitySection } from "./_components/availability-section";

export default function TrainerSettingsPage() {
  const t = useTranslations("trainerSettings");
  const router = useRouter();

  // Een Admin komt hier alleen als hij zelf lesgeeft (AdminsActAsTrainers); anders terug naar het dashboard.
  const isAdmin = useClientValue(() => getAuthUser()?.role ?? null) === "Admin";
  const { data: settings } = useQuery({
    queryKey: ["organizationSettings"],
    queryFn: getOrganizationSettings,
    enabled: isAdmin,
    staleTime: 5 * 60 * 1000,
  });
  const adminNotTeaching = isAdmin && settings !== undefined && !settings.adminsActAsTrainers;
  useEffect(() => {
    if (adminNotTeaching) router.replace("/dashboard");
  }, [adminNotTeaching, router]);

  if (isAdmin && (settings === undefined || adminNotTeaching)) return null;

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          {t("title")}
        </h1>
        <p className="text-gray-400 text-sm mt-1">{t("subtitle")}</p>
      </div>

      <AvailabilitySection />
    </div>
  );
}
