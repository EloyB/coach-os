"use client";

import { useId } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getCampById } from "@/lib/api/camps";
import { inputClass } from "@/lib/styles";

interface CampPriceSelectProps {
  campId: string;
  /** Gekozen tarief; null = hoofdprijs. */
  value: string | null;
  onChange: (priceId: string | null) => void;
}

/**
 * Keuze van het tarief voor één deelnemer (hoofdprijs of een van de prijsopties van het kamp).
 * Rendert niets zolang het kamp geen prijsopties heeft.
 */
export function CampPriceSelect({
  campId,
  value,
  onChange,
}: CampPriceSelectProps) {
  const t = useTranslations("campEnrollmentsTable");
  const id = useId();
  const { data: camp } = useQuery({
    queryKey: ["camp", campId],
    queryFn: () => getCampById(campId),
  });

  if (!camp || camp.prices.length === 0) return null;

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium">
        {t("priceLabel")}
      </label>
      <select
        id={id}
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value || null)}
        className={inputClass + " mt-1"}
      >
        <option value="">{t("priceMain", { amount: camp.price })}</option>
        {camp.prices.map((p) => (
          <option key={p.id} value={p.id}>
            {p.label} (€{p.amount})
          </option>
        ))}
      </select>
    </div>
  );
}
