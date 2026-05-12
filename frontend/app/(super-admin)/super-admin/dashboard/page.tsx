"use client";

import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { listAdmins, listOrganizations } from "@/lib/api/super-admin";

function StatCard({ label, value, accent }: { label: string; value: number | string; accent: string }) {
  return (
    <div className="bg-white rounded-lg border-l-4 p-5 shadow-sm" style={{ borderLeftColor: accent }}>
      <div className="text-xs uppercase tracking-wide text-ink-3 mb-1">{label}</div>
      <div className="text-3xl font-bold text-ink">{value}</div>
    </div>
  );
}

export default function SuperAdminDashboardPage() {
  const t = useTranslations("superAdmin");

  const { data: admins } = useQuery({
    queryKey: ["super-admin", "admins"],
    queryFn: listAdmins,
  });
  const { data: orgs } = useQuery({
    queryKey: ["super-admin", "organizations"],
    queryFn: listOrganizations,
  });

  const adminCount = admins?.length ?? 0;
  const pendingInvites = admins?.filter((a) => a.invitePending).length ?? 0;
  const orgCount = orgs?.length ?? 0;
  const earlyBirdCount = orgs?.filter((o) => o.isEarlyBird).length ?? 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">{t("dashboardTitle")}</h1>
        <p className="text-sm text-ink-3 mt-1">{t("dashboardSubtitle")}</p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard label={t("statAdmins")} value={adminCount} accent="#2D5016" />
        <StatCard label={t("statPendingInvites")} value={pendingInvites} accent="#D0FF14" />
        <StatCard label={t("statOrganizations")} value={orgCount} accent="#2D5016" />
        <StatCard label={t("statEarlyBirds")} value={earlyBirdCount} accent="#D0FF14" />
      </div>
    </div>
  );
}
