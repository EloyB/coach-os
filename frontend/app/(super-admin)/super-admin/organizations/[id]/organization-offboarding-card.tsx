"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  deleteOrganization,
  getOrganizationOffboarding,
  type OffboardingBlocker,
  type OrganizationDataCounts,
} from "@/lib/api/privacy";
import { formatDateNL } from "@/lib/date-utils";
import { inputClass } from "@/lib/styles";

const COUNT_KEYS: (keyof OrganizationDataCounts)[] = [
  "tennisClubs",
  "lessonSeries",
  "lessons",
  "enrollments",
  "camps",
  "campEnrollments",
  "payments",
  "memberships",
  "accountsToDelete",
  "subscriptionPayments",
];

interface OrganizationOffboardingCardProps {
  organizationId: string;
}

/** Manuele offboarding: preview (zonder locks) en definitieve verwijdering met naambevestiging. */
export function OrganizationOffboardingCard({ organizationId }: OrganizationOffboardingCardProps) {
  const t = useTranslations("privacy");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [confirmationName, setConfirmationName] = useState("");

  const { data: preview, isLoading } = useQuery({
    queryKey: ["super-admin", "offboarding", organizationId],
    queryFn: () => getOrganizationOffboarding(organizationId),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteOrganization(organizationId, confirmationName),
    onSuccess: () => {
      toast.success(t("deleted"));
      queryClient.invalidateQueries({ queryKey: ["super-admin"] });
      router.push("/super-admin/organizations");
    },
  });

  if (isLoading || !preview) {
    return <p className="text-sm text-ink-3">{t("loading")}</p>;
  }

  const nameMatches = confirmationName === preview.organizationName;

  return (
    <section className="rounded-lg border border-rule bg-white p-6">
      <h2 className="text-base font-bold text-ink">{t("offboardingTitle")}</h2>
      <p className="mt-1 text-sm text-ink-3">{t("offboardingSubtitle")}</p>

      <dl className="mt-4 grid grid-cols-2 gap-x-4 gap-y-1 text-sm sm:grid-cols-3">
        <dt className="text-ink-3">{t("endedOn")}</dt>
        <dd className="text-ink sm:col-span-2">{preview.endedOn ? formatDateNL(preview.endedOn) : t("noEnd")}</dd>
        <dt className="text-ink-3">{t("deletableFrom")}</dt>
        <dd className="text-ink sm:col-span-2">
          {preview.deletableFrom ? formatDateNL(preview.deletableFrom) : t("noEnd")}
        </dd>
      </dl>

      <ul className="mt-4 grid grid-cols-2 gap-2 text-sm sm:grid-cols-3">
        {COUNT_KEYS.map((key) => (
          <li key={key} className="rounded-md bg-gray-50 px-3 py-2">
            <span className="block text-xs text-ink-3">{t(`counts.${key}`)}</span>
            <span className="font-semibold text-ink">{preview.counts[key]}</span>
          </li>
        ))}
      </ul>

      {preview.eligible ? (
        <DeleteConfirmation
          organizationName={preview.organizationName}
          value={confirmationName}
          onChange={setConfirmationName}
          disabled={!nameMatches || deleteMutation.isPending}
          onDelete={() => deleteMutation.mutate()}
        />
      ) : (
        <BlockerList blockers={preview.blockers} />
      )}
    </section>
  );
}

function BlockerList({ blockers }: { blockers: OffboardingBlocker[] }) {
  const t = useTranslations("privacy");
  return (
    <div className="mt-4 rounded-md bg-amber-50 p-3 text-sm text-amber-900">
      <p className="font-semibold">{t("blockedTitle")}</p>
      <ul className="mt-1 list-disc pl-5">
        {blockers.map((blocker) => (
          <li key={blocker.code}>
            {t(`blockers.${blocker.code}`, { date: blocker.date ? formatDateNL(blocker.date) : "—" })}
          </li>
        ))}
      </ul>
    </div>
  );
}

interface DeleteConfirmationProps {
  organizationName: string;
  value: string;
  onChange: (value: string) => void;
  disabled: boolean;
  onDelete: () => void;
}

function DeleteConfirmation({ organizationName, value, onChange, disabled, onDelete }: DeleteConfirmationProps) {
  const t = useTranslations("privacy");
  return (
    <div className="mt-4 space-y-3 border-t border-rule pt-4">
      <p className="text-sm text-red-700">{t("deleteWarning")}</p>
      <label className="block text-xs font-medium text-ink-2" htmlFor="offboarding-confirm">
        {t("confirmLabel", { name: organizationName })}
      </label>
      <input
        id="offboarding-confirm"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        maxLength={200}
        autoComplete="off"
        className={inputClass}
      />
      <Button className="bg-red-600 hover:bg-red-700" disabled={disabled} onClick={onDelete}>
        {t("deleteCta")}
      </Button>
    </div>
  );
}
