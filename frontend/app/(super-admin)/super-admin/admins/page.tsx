"use client";

import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  disableAdmin,
  enableAdmin,
  listAdmins,
  resendAdminInvite,
  type AdminListItemDto,
} from "@/lib/api/super-admin";
import { Button } from "@/components/ui/button";

export default function AdminsPage() {
  const t = useTranslations("superAdmin");
  const queryClient = useQueryClient();

  const { data: admins, isLoading } = useQuery({
    queryKey: ["super-admin", "admins"],
    queryFn: listAdmins,
  });

  const disableMutation = useMutation({
    mutationFn: (userId: string) => disableAdmin(userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["super-admin", "admins"] }),
  });
  const enableMutation = useMutation({
    mutationFn: (userId: string) => enableAdmin(userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["super-admin", "admins"] }),
  });
  const resendMutation = useMutation({
    mutationFn: (userId: string) => resendAdminInvite(userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["super-admin", "admins"] }),
  });

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-bold text-ink">{t("adminsTitle")}</h1>
          <p className="text-sm text-ink-3 mt-1">{t("adminsSubtitle")}</p>
        </div>
        <Button asChild className="bg-tennis-green hover:bg-tennis-green/90 text-white">
          <Link href="/super-admin/admins/new">{t("adminsCreate")}</Link>
        </Button>
      </div>

      {isLoading ? (
        <div className="text-ink-3 text-sm">{t("loading")}</div>
      ) : (
        <div className="bg-white rounded-lg border border-rule overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-canvas text-ink-3 text-xs uppercase tracking-wide">
              <tr>
                <th className="px-4 py-3 text-left">{t("colName")}</th>
                <th className="px-4 py-3 text-left">{t("colEmail")}</th>
                <th className="px-4 py-3 text-left">{t("colOrganization")}</th>
                <th className="px-4 py-3 text-left">{t("colEarlyBird")}</th>
                <th className="px-4 py-3 text-left">{t("colStatus")}</th>
                <th className="px-4 py-3 text-right">{t("colActions")}</th>
              </tr>
            </thead>
            <tbody>
              {admins?.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-ink-3">
                    {t("adminsEmpty")}
                  </td>
                </tr>
              )}
              {admins?.map((admin) => (
                <AdminRow
                  key={admin.userId}
                  admin={admin}
                  onDisable={() => disableMutation.mutate(admin.userId)}
                  onEnable={() => enableMutation.mutate(admin.userId)}
                  onResend={() => resendMutation.mutate(admin.userId)}
                  busy={
                    disableMutation.isPending ||
                    enableMutation.isPending ||
                    resendMutation.isPending
                  }
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function AdminRow({
  admin,
  onDisable,
  onEnable,
  onResend,
  busy,
}: {
  admin: AdminListItemDto;
  onDisable: () => void;
  onEnable: () => void;
  onResend: () => void;
  busy: boolean;
}) {
  const t = useTranslations("superAdmin");
  const orgs = admin.organizations.map((o) => o.organizationName).join(", ");
  const isEarlyBird = admin.organizations.some((o) => o.isEarlyBird);

  let statusLabel: string;
  let statusClass: string;
  if (admin.invitePending) {
    statusLabel = t("statusPending");
    statusClass = "bg-yellow-100 text-yellow-800";
  } else if (admin.isActive) {
    statusLabel = t("statusActive");
    statusClass = "bg-green-100 text-green-800";
  } else {
    statusLabel = t("statusDisabled");
    statusClass = "bg-red-100 text-red-800";
  }

  return (
    <tr className="border-t border-rule">
      <td className="px-4 py-3 font-medium text-ink">
        {admin.firstName} {admin.lastName}
      </td>
      <td className="px-4 py-3 text-ink-2">{admin.email}</td>
      <td className="px-4 py-3 text-ink-2">{orgs || "—"}</td>
      <td className="px-4 py-3">
        {isEarlyBird ? (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-tennis-lime/30 text-tennis-green">
            ★ {t("earlyBirdYes")}
          </span>
        ) : (
          <span className="text-xs text-ink-3">{t("earlyBirdNo")}</span>
        )}
      </td>
      <td className="px-4 py-3">
        <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${statusClass}`}>
          {statusLabel}
        </span>
      </td>
      <td className="px-4 py-3 text-right space-x-2">
        {admin.invitePending && (
          <Button size="sm" variant="outline" disabled={busy} onClick={onResend}>
            {t("actionResend")}
          </Button>
        )}
        {admin.isActive ? (
          <Button size="sm" variant="outline" disabled={busy} onClick={onDisable}>
            {t("actionDisable")}
          </Button>
        ) : (
          <Button size="sm" variant="outline" disabled={busy} onClick={onEnable}>
            {t("actionEnable")}
          </Button>
        )}
      </td>
    </tr>
  );
}
