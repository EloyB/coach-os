"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  listOrganizations,
  setOrganizationEarlyBird,
} from "@/lib/api/super-admin";

export default function OrganizationsPage() {
  const t = useTranslations("superAdmin");
  const queryClient = useQueryClient();

  const { data: orgs, isLoading } = useQuery({
    queryKey: ["super-admin", "organizations"],
    queryFn: listOrganizations,
  });

  const toggleEarlyBird = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) =>
      setOrganizationEarlyBird(id, value),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["super-admin", "organizations"] }),
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">{t("orgsTitle")}</h1>
        <p className="text-sm text-ink-3 mt-1">{t("orgsSubtitle")}</p>
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
                <th className="px-4 py-3 text-left">{t("colAdmins")}</th>
                <th className="px-4 py-3 text-left">{t("colEarlyBird")}</th>
              </tr>
            </thead>
            <tbody>
              {orgs?.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-ink-3">
                    {t("orgsEmpty")}
                  </td>
                </tr>
              )}
              {orgs?.map((org) => (
                <tr key={org.id} className="border-t border-rule">
                  <td className="px-4 py-3 font-medium text-ink">{org.name}</td>
                  <td className="px-4 py-3 text-ink-2">{org.email}</td>
                  <td className="px-4 py-3 text-ink-2">{org.adminCount}</td>
                  <td className="px-4 py-3">
                    <label className="inline-flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={org.isEarlyBird}
                        disabled={toggleEarlyBird.isPending}
                        onChange={(e) =>
                          toggleEarlyBird.mutate({ id: org.id, value: e.target.checked })
                        }
                        className="h-4 w-4"
                      />
                      <span className="text-sm text-ink-2">
                        {org.isEarlyBird ? t("earlyBirdYes") : t("earlyBirdNo")}
                      </span>
                    </label>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
