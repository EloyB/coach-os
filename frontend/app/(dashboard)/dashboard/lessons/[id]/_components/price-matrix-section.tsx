"use client";

import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Euro, Info } from "lucide-react";
import {
  getLessonSeriePrices,
  saveLessonSeriePrices,
  CATEGORY_LABELS,
  GROUP_SIZES,
  PARTICIPANT_CATEGORIES,
  type LessonSeriePriceRequest,
} from "@/lib/api/lessonSeriePrices";
import { inputClass } from "@/lib/styles";

/** Sleutel voor één cel in de matrix. */
function cellKey(category: number, groupSize: number): string {
  return `${category}-${groupSize}`;
}

/**
 * Prijsmatrix per categorie × groepsgrootte. Elk bedrag is het TOTAAL voor de
 * hele groep, niet per persoon — dat verschil is bewust prominent in de UI,
 * omdat het legacy prijsveld op de reeks wél per persoon geldt.
 */
export function PriceMatrixSection({
  seriesId,
  legacyPrice,
}: {
  seriesId: string;
  legacyPrice: number;
}) {
  const queryClient = useQueryClient();
  const [values, setValues] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [dirty, setDirty] = useState(false);

  const { data: prices, isLoading } = useQuery({
    queryKey: ["lessonSeriePrices", seriesId],
    queryFn: () => getLessonSeriePrices(seriesId),
  });

  useEffect(() => {
    if (!prices) return;
    const next: Record<string, string> = {};
    for (const p of prices) {
      next[cellKey(p.category, p.groupSize)] = String(p.totalPrice);
    }
    setValues(next);
    setDirty(false);
  }, [prices]);

  function handleChange(category: number, groupSize: number, raw: string) {
    setValues((prev) => ({ ...prev, [cellKey(category, groupSize)]: raw }));
    setDirty(true);
  }

  async function handleSave() {
    const payload: LessonSeriePriceRequest[] = [];

    for (const category of Object.values(PARTICIPANT_CATEGORIES)) {
      for (const groupSize of GROUP_SIZES) {
        const raw = values[cellKey(category, groupSize)];
        if (raw === undefined || raw.trim() === "") continue;

        const parsed = Number(raw);
        if (Number.isNaN(parsed) || parsed < 0) {
          toast.error(
            `Ongeldig bedrag bij ${CATEGORY_LABELS[category]}, groep van ${groupSize}.`,
          );
          return;
        }
        payload.push({ category, groupSize, totalPrice: parsed });
      }
    }

    setSaving(true);
    try {
      await saveLessonSeriePrices(seriesId, payload);
      toast.success(
        payload.length === 0
          ? "Prijstabel gewist — de reeks gebruikt weer de standaardprijs"
          : "Prijstabel opgeslagen",
      );
      queryClient.invalidateQueries({ queryKey: ["lessonSeriePrices", seriesId] });
      setDirty(false);
    } catch {
      // Foutmelding komt al van de axios interceptor
    } finally {
      setSaving(false);
    }
  }

  const hasAnyValue = Object.values(values).some((v) => v?.trim() !== "");

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <Euro size={14} className="text-tennis-green" />
          <h2 className="text-sm font-semibold text-gray-800">Prijzen</h2>
        </div>
        {dirty && (
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-3 py-1.5 rounded-lg bg-tennis-green text-white text-xs font-medium hover:bg-tennis-green/90 transition-colors disabled:opacity-50"
          >
            {saving ? "Bezig…" : "Opslaan"}
          </button>
        )}
      </div>

      {isLoading ? (
        <div className="p-8 text-center">
          <div className="w-4 h-4 border-2 border-tennis-green/30 border-t-tennis-green rounded-full animate-spin mx-auto" />
        </div>
      ) : (
        <div className="p-5">
          <div className="flex items-start gap-2 mb-4 px-3 py-2 bg-blue-50 border border-blue-100 rounded-lg">
            <Info size={12} className="text-blue-600 shrink-0 mt-0.5" />
            <p className="text-xs text-blue-800">
              Elk bedrag is het <strong>totaal voor de hele groep</strong>, niet per
              persoon. Bij een gemengde groep wordt per deelnemer het aandeel van zijn
              categorie gerekend. Laat velden leeg om ze niet in te stellen.
            </p>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left py-2 pr-4 text-xs font-medium text-gray-500">
                    Groepsgrootte
                  </th>
                  {Object.values(PARTICIPANT_CATEGORIES).map((category) => (
                    <th
                      key={category}
                      className="text-left py-2 px-2 text-xs font-medium text-gray-500"
                    >
                      {CATEGORY_LABELS[category]}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {GROUP_SIZES.map((groupSize) => (
                  <tr key={groupSize} className="border-b border-gray-50 last:border-b-0">
                    <td className="py-2 pr-4 text-gray-700">
                      {groupSize === 1 ? "Privé (1)" : `${groupSize} personen`}
                    </td>
                    {Object.values(PARTICIPANT_CATEGORIES).map((category) => (
                      <td key={category} className="py-2 px-2">
                        <div className="relative w-32">
                          <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-gray-400">
                            €
                          </span>
                          <input
                            type="number"
                            min={0}
                            step={0.01}
                            aria-label={`${CATEGORY_LABELS[category]}, groep van ${groupSize}`}
                            value={values[cellKey(category, groupSize)] ?? ""}
                            onChange={(e) =>
                              handleChange(category, groupSize, e.target.value)
                            }
                            placeholder="—"
                            className={inputClass + " pl-6"}
                          />
                        </div>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {!hasAnyValue && (
            <p className="mt-4 text-xs text-gray-500">
              Geen prijstabel ingesteld. De reeks rekent nu{" "}
              <strong>€{legacyPrice} per deelnemer</strong>.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
