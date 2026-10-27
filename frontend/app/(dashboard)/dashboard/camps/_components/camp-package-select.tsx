"use client";

import { useId } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getCampById } from "@/lib/api/camps";
import { inputClass } from "@/lib/styles";

interface CampPackageSelectProps {
  campId: string;
  /** Gekozen pakket; null = hele kamp tegen de standaardprijs. */
  value: string | null;
  onChange: (packageId: string | null) => void;
}

/**
 * Keuze van het pakket voor één deelnemer (hele kamp of een van de pakketten van het kamp).
 * Rendert niets zolang het kamp geen pakketten heeft.
 */
export function CampPackageSelect({
  campId,
  value,
  onChange,
}: CampPackageSelectProps) {
  const t = useTranslations("camps");
  const id = useId();
  const { data: camp } = useQuery({
    queryKey: ["camp", campId],
    queryFn: () => getCampById(campId),
  });

  if (!camp || camp.packages.length === 0) return null;

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium">
        {t("packageLabel")}
      </label>
      <select
        id={id}
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value || null)}
        className={inputClass + " mt-1"}
      >
        <option value="">{t("packageWholeCamp", { amount: camp.price })}</option>
        {camp.packages.map((p) => (
          <option key={p.id} value={p.id}>
            {p.name} (€{p.price})
          </option>
        ))}
      </select>
    </div>
  );
}
