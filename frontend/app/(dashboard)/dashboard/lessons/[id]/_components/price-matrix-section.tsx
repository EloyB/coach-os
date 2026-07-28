"use client";

import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Euro, Info, Plus, Trash2 } from "lucide-react";
import {
  PRICING_MODES,
  getLessonSeriePrices,
  saveLessonSeriePrices,
  type LessonSeriePriceDto,
  type LessonSeriePriceRequest,
} from "@/lib/api/lessonSeriePrices";
import { inputClass } from "@/lib/styles";

type PriceDraft = {
  id: string;
  label: string;
  description: string;
  totalPrice: string;
};

function toDraft(p: LessonSeriePriceDto, index: number): PriceDraft {
  return {
    id: p.id || `saved-${index}`,
    label: p.label,
    description: p.description ?? "",
    totalPrice: String(p.totalPrice),
  };
}

function newDraft(): PriceDraft {
  return {
    id: `new-${crypto.randomUUID()}`,
    label: "",
    description: "",
    totalPrice: "",
  };
}

/**
 * Prijsopties per lessenreeks. Bewust simpel gehouden: een reeks heeft een lijst
 * benoemde tarieven (naam + beschrijving + prijs). De speler kiest er bij het
 * inschrijven één; is er maar één, dan is die automatisch gekozen. Zonder opties
 * rekent de reeks de standaardprijs per deelnemer. De prijs geldt per deelnemer.
 */
export function PriceMatrixSection({
  seriesId,
  legacyPrice,
}: {
  seriesId: string;
  legacyPrice: number;
}) {
  const queryClient = useQueryClient();
  const [drafts, setDrafts] = useState<PriceDraft[]>([]);
  const [saving, setSaving] = useState(false);
  const [dirty, setDirty] = useState(false);

  const { data: prices, isLoading } = useQuery({
    queryKey: ["lessonSeriePrices", seriesId],
    queryFn: () => getLessonSeriePrices(seriesId),
  });

  useEffect(() => {
    if (!prices) return;
    setDrafts(prices.map(toDraft));
    setDirty(false);
  }, [prices]);

  function update(id: string, patch: Partial<PriceDraft>) {
    setDrafts((prev) => prev.map((p) => (p.id === id ? { ...p, ...patch } : p)));
    setDirty(true);
  }

  function add() {
    setDrafts((prev) => [...prev, newDraft()]);
    setDirty(true);
  }

  function remove(id: string) {
    setDrafts((prev) => prev.filter((p) => p.id !== id));
    setDirty(true);
  }

  async function handleSave() {
    const payload: LessonSeriePriceRequest[] = [];

    for (const [index, draft] of drafts.entries()) {
      if (!draft.label.trim()) {
        toast.error("Elke prijsoptie heeft een naam nodig.");
        return;
      }
      const amount = Number(draft.totalPrice);
      if (Number.isNaN(amount) || amount < 0) {
        toast.error(`Ongeldig bedrag bij ${draft.label}.`);
        return;
      }

      payload.push({
        label: draft.label.trim(),
        description: draft.description.trim() || null,
        // Eén simpel model: elke optie is een benoemd tarief waaruit de speler kiest.
        mode: PRICING_MODES.ManualOption,
        category: null,
        groupSize: null,
        totalPrice: amount,
        sortOrder: index,
        reusableKey: null,
      });
    }

    setSaving(true);
    try {
      await saveLessonSeriePrices(seriesId, payload);
      toast.success(
        payload.length === 0
          ? "Prijsopties gewist — de reeks gebruikt weer de standaardprijs"
          : "Prijsopties opgeslagen",
      );
      queryClient.invalidateQueries({ queryKey: ["lessonSeriePrices", seriesId] });
      setDirty(false);
    } catch {
      // Foutmelding komt al van de axios interceptor
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <Euro size={14} className="text-tennis-green" />
          <div>
            <h2 className="text-sm font-semibold text-gray-800">Prijsopties</h2>
            <p className="text-xs text-gray-500">
              Tarieven voor deze reeks — de speler kiest er één bij het inschrijven.
            </p>
          </div>
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
        <div className="p-5 space-y-4">
          <div className="flex items-start gap-2 px-3 py-2 bg-blue-50 border border-blue-100 rounded-lg">
            <Info size={12} className="text-blue-600 shrink-0 mt-0.5" />
            <p className="text-xs text-blue-800">
              Voeg de tarieven toe die voor deze reeks gelden. De prijs geldt per deelnemer.
              Laat je de lijst leeg, dan rekent de reeks de standaardprijs van €{legacyPrice} per deelnemer.
            </p>
          </div>

          {drafts.length > 0 && (
            <div className="space-y-3">
              {drafts.map((option) => (
                <div
                  key={option.id}
                  className="rounded-lg border border-gray-200 bg-white p-3.5 space-y-3"
                >
                  {/* Regel 1 — naam + prijs + verwijderen */}
                  <div className="flex flex-wrap items-end gap-3">
                    <div className="flex-1 min-w-[180px]">
                      <label className="block text-xs font-medium text-gray-600 mb-1">Naam</label>
                      <input
                        className={inputClass}
                        value={option.label}
                        placeholder="bv. Standaardtarief, Jeugd, Duo"
                        onChange={(e) => update(option.id, { label: e.target.value })}
                      />
                    </div>
                    <div className="w-32">
                      <label className="block text-xs font-medium text-gray-600 mb-1">Prijs</label>
                      <div className="relative">
                        <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-gray-400">€</span>
                        <input
                          type="number"
                          min={0}
                          step={0.01}
                          className={inputClass + " pl-6"}
                          value={option.totalPrice}
                          onChange={(e) => update(option.id, { totalPrice: e.target.value })}
                        />
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => remove(option.id)}
                      className="shrink-0 inline-flex items-center justify-center w-9 h-9 rounded-lg text-red-500 hover:bg-red-50 transition-colors"
                      aria-label={`Verwijder ${option.label || "prijsoptie"}`}
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>

                  {/* Regel 2 — beschrijving over de volle breedte */}
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Beschrijving</label>
                    <textarea
                      className={inputClass + " min-h-16"}
                      value={option.description}
                      placeholder="Optioneel — voor wie of wanneer geldt dit tarief?"
                      onChange={(e) => update(option.id, { description: e.target.value })}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}

          <button
            type="button"
            onClick={add}
            className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg border border-dashed border-gray-300 text-xs font-medium text-gray-600 hover:border-tennis-green hover:text-tennis-green transition-colors"
          >
            <Plus size={14} /> Optie toevoegen
          </button>

          {drafts.length === 0 && (
            <p className="text-xs text-gray-500">
              Nog geen prijsopties. De reeks rekent nu <strong>€{legacyPrice} per deelnemer</strong>.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
