"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Layers, Plus } from "lucide-react";
import { summarizePackageSlots } from "@/lib/camp-packages";
import { CampPackageDialog } from "./camp-package-dialog";
import type { CampDayDraft, CampPackageDraft } from "../new/_types";

export const MAX_PACKAGES = 10;

/** Dagen als `PackageDay` (de dagdeelpositie is de index in de lijst). */
export function toPackageDays(days: CampDayDraft[]) {
  return days.map((d) => ({
    date: d.date,
    parts: d.parts.map((p, order) => ({ order, ...p })),
  }));
}

interface CampPackagesEditorProps {
  days: CampDayDraft[];
  packages: CampPackageDraft[];
  /** Past de lijst toe (en bewaart ze eventueel); geeft foutmeldingen terug (leeg = gelukt). */
  onChange: (packages: CampPackageDraft[]) => Promise<string[]>;
  canEdit?: boolean;
}

/** Lijst van pakketten met een dialog om ze toe te voegen, aan te passen of te verwijderen. */
export function CampPackagesEditor({
  days,
  packages,
  onChange,
  canEdit = true,
}: CampPackagesEditorProps) {
  const t = useTranslations("camps");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [pending, setPending] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const everyDayHasParts = days.length > 0 && days.every((d) => d.parts.length > 0);
  const packageDays = toPackageDays(days);

  function open(index: number | null) {
    setErrors([]);
    setEditingIndex(index);
    setDialogOpen(true);
  }

  async function apply(next: CampPackageDraft[]) {
    setPending(true);
    const result = await onChange(next);
    setPending(false);
    setErrors(result);
    if (result.length === 0) setDialogOpen(false);
  }

  const editing = editingIndex !== null ? (packages[editingIndex] ?? null) : null;

  return (
    <div>
      {!everyDayHasParts && packages.length === 0 && (
        <p className="mb-3 text-xs text-gray-500">{t("packagesNeedParts")}</p>
      )}

      {packages.length > 0 && (
        <ul className="mb-3 space-y-2.5">
          {packages.map((p, index) => {
            const lines = summarizePackageSlots(packageDays, p.slots);
            return (
              <li key={p.id ?? index}>
                <button
                  type="button"
                  disabled={!canEdit}
                  onClick={() => open(index)}
                  className="w-full rounded-lg border border-gray-200 p-3 text-left transition-colors enabled:hover:border-tennis-green/50 enabled:hover:bg-gray-50"
                >
                  <div className="flex items-baseline justify-between gap-3">
                    <span className="text-sm font-semibold text-gray-900">{p.name}</span>
                    <span className="shrink-0 text-sm font-semibold text-gray-900">
                      {t("packagePricePerParticipant", { price: p.price })}
                    </span>
                  </div>
                  {p.description && (
                    <p className="mt-0.5 text-xs text-gray-500">{p.description}</p>
                  )}
                  <ul className="mt-1.5 space-y-0.5 text-xs text-gray-500">
                    {lines.length === 0 ? (
                      <li className="text-red-600">{t("packageNoSlotsSummary")}</li>
                    ) : (
                      lines.map((line) => (
                        <li key={line} className="first-letter:uppercase">
                          {line}
                        </li>
                      ))
                    )}
                  </ul>
                </button>
              </li>
            );
          })}
        </ul>
      )}

      {canEdit && packages.length < MAX_PACKAGES && everyDayHasParts && (
        <button
          type="button"
          onClick={() => open(null)}
          className="inline-flex items-center gap-1.5 text-sm font-medium text-tennis-green hover:underline"
        >
          <Plus size={14} />
          {t("packageAdd")}
        </button>
      )}

      {canEdit && (
        <CampPackageDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          days={days}
          pkg={editing}
          pending={pending}
          errorMessages={errors}
          onSubmit={(pkg) =>
            apply(
              editingIndex !== null
                ? packages.map((p, i) => (i === editingIndex ? { ...pkg, id: p.id } : p))
                : [...packages, pkg],
            )
          }
          onDelete={
            editingIndex !== null
              ? () => apply(packages.filter((_, i) => i !== editingIndex))
              : undefined
          }
        />
      )}
    </div>
  );
}

/** Kop voor het pakkettenblok in een kaart. */
export function CampPackagesHeading() {
  const t = useTranslations("camps");
  return (
    <div className="flex items-center gap-2.5">
      <div className="flex h-6 w-6 items-center justify-center rounded-md bg-tennis-green/10">
        <Layers size={13} className="text-tennis-green" />
      </div>
      <div>
        <h2 className="text-sm font-semibold text-gray-900">{t("packagesTitle")}</h2>
        <p className="mt-0.5 text-xs text-gray-400">{t("packagesHint")}</p>
      </div>
    </div>
  );
}
