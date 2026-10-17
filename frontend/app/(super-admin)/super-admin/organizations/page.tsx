"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  extendOrganizationTrial,
  listOrganizations,
  setOrganizationEarlyBird,
  type OrganizationListItemDto,
} from "@/lib/api/super-admin";

const TRIAL_EXTENSION_DAYS = 14;

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-");
  return `${day}/${month}/${year}`;
}

function isPast(isoDate: string): boolean {
  const today = new Date();
  const todayIso = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
  return isoDate < todayIso;
}

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
    onSuccess: () => {
      // Early-bird zit ook in de admins-lijst (kolom + sterretje) — invalidate beide
      // zodat de waarde meteen klopt zodra je naar de andere tab gaat.
      queryClient.invalidateQueries({ queryKey: ["super-admin", "organizations"] });
      queryClient.invalidateQueries({ queryKey: ["super-admin", "admins"] });
    },
  });

  const extendTrial = useMutation({
    mutationFn: (id: string) => extendOrganizationTrial(id, TRIAL_EXTENSION_DAYS),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["super-admin", "organizations"] });
    },
  });

  function subscriptionLabel(org: OrganizationListItemDto): string {
    if (org.subscriptionStatus === "Trialing" && org.trialEndsAt) {
      const date = formatDate(org.trialEndsAt);
      return isPast(org.trialEndsAt)
        ? t("subscriptionTrialExpired", { date })
        : t("subscriptionTrialUntil", { date });
    }
    return org.subscriptionStatus === "Active" ? t("subscriptionActive") : t("subscriptionLegacy");
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">{t("orgsTitle")}</h1>
        <p className="text-sm text-ink-3 mt-1">{t("orgsSubtitle")}</p>
      </div>

      {isLoading ? (
        <div className="text-ink-3 text-sm">{t("loading")}</div>
      ) : (
        <div className="bg-white rounded-lg border border-rule overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-canvas text-ink-3 text-xs uppercase tracking-wide">
              <tr>
                <th className="px-4 py-3 text-left">{t("colName")}</th>
                <th className="px-4 py-3 text-left">{t("colEmail")}</th>
                <th className="px-4 py-3 text-left">{t("colAdmins")}</th>
                <th className="px-4 py-3 text-left">{t("colSubscription")}</th>
                <th className="px-4 py-3 text-left">{t("colEarlyBird")}</th>
              </tr>
            </thead>
            <tbody>
              {orgs?.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-ink-3">
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
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-ink-2">{subscriptionLabel(org)}</span>
                      {org.subscriptionStatus === "Trialing" && (
                        <button
                          type="button"
                          onClick={() => extendTrial.mutate(org.id)}
                          disabled={extendTrial.isPending}
                          className="px-2 py-1 text-xs font-semibold text-tennis-green border border-tennis-green/30 rounded hover:bg-tennis-green/5 cursor-pointer disabled:opacity-60"
                        >
                          {t("extendTrial", { days: TRIAL_EXTENSION_DAYS })}
                        </button>
                      )}
                    </div>
                  </td>
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
