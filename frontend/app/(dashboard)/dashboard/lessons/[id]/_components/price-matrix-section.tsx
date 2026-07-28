"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Euro, Info, Plus, Trash2 } from "lucide-react";
import {
  CATEGORY_LABELS,
  GROUP_SIZES,
  PARTICIPANT_CATEGORIES,
  PRICING_MODE_LABELS,
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
  mode: number;
  category: string;
  groupSize: string;
  totalPrice: string;
  reusableKey: string;
};

function toDraft(p: LessonSeriePriceDto, index: number): PriceDraft {
  return {
    id: p.id || `saved-${index}`,
    label: p.label,
    description: p.description ?? "",
    mode: p.mode,
    category: p.category ? String(p.category) : "",
    groupSize: p.groupSize ? String(p.groupSize) : "",
    totalPrice: String(p.totalPrice),
    reusableKey: p.reusableKey ?? "",
  };
}

function newDraft(mode: number = PRICING_MODES.FixedPerParticipant): PriceDraft {
  return {
    id: `new-${crypto.randomUUID()}`,
    label: "",
    description: "",
    mode,
    category: "",
    groupSize: mode === PRICING_MODES.GroupSize ? "4" : "",
    totalPrice: "",
    reusableKey: "",
  };
}

function summary(option: PriceDraft): string {
  if (option.mode === PRICING_MODES.GroupSize && option.groupSize) {
    return `Totaalprijs voor groep van ${option.groupSize}`;
  }
  if (option.mode === PRICING_MODES.TariffCategory && option.category) {
    return `Per deelnemer: ${CATEGORY_LABELS[Number(option.category)]}`;
  }
  if (option.mode === PRICING_MODES.ManualOption) {
    return "Speler kiest deze optie zelf";
  }
  return "Per deelnemer";
}

/**
 * Flexibele prijsopties per lessenreeks. Vervangt de oude vaste matrix als hoofd-UX:
 * admins kiezen zelf welke opties bestaan en leggen met de beschrijving uit wanneer
 * een prijs geldt. ReusableKey is de lichte eerste stap naar organisatiebrede templates.
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

  const grouped = useMemo(() => {
    return Object.entries(PRICING_MODE_LABELS).map(([mode, label]) => ({
      mode: Number(mode),
      label,
      options: drafts.filter((d) => d.mode === Number(mode)),
    }));
  }, [drafts]);

  function update(id: string, patch: Partial<PriceDraft>) {
    setDrafts((prev) => prev.map((p) => {
      if (p.id !== id) return p;
      const next = { ...p, ...patch };
      if (patch.mode && patch.mode !== PRICING_MODES.GroupSize) next.groupSize = "";
      if (patch.mode && patch.mode !== PRICING_MODES.TariffCategory) next.category = "";
      return next;
    }));
    setDirty(true);
  }

  function add(mode: number) {
    setDrafts((prev) => [...prev, newDraft(mode)]);
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
        mode: draft.mode,
        category: draft.category ? Number(draft.category) : null,
        groupSize: draft.groupSize ? Number(draft.groupSize) : null,
        totalPrice: amount,
        sortOrder: index,
        reusableKey: draft.reusableKey.trim() || null,
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
              Vervangt de vaste matrix: leg per optie uit wanneer welke prijs geldt.
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
        <div className="p-5 space-y-5">
          <div className="flex items-start gap-2 px-3 py-2 bg-blue-50 border border-blue-100 rounded-lg">
            <Info size={12} className="text-blue-600 shrink-0 mt-0.5" />
            <p className="text-xs text-blue-800">
              Maak alleen de prijsopties die voor deze reeks gelden. Gebruik de beschrijving
              om duidelijk te maken of een prijs automatisch geldt of door de speler gekozen wordt.
              De sleutel “herbruikbaar als” kan later gebruikt worden om opties per organisatie te hergebruiken.
            </p>
          </div>

          {grouped.map((group) => (
            <section key={group.mode} className="border border-gray-100 rounded-xl overflow-hidden">
              <div className="px-4 py-3 bg-gray-50 flex items-center justify-between">
                <div>
                  <h3 className="text-xs font-semibold text-gray-700">{group.label}</h3>
                  <p className="text-[11px] text-gray-500">
                    {group.mode === PRICING_MODES.GroupSize && "Totaalbedrag voor de hele groep."}
                    {group.mode === PRICING_MODES.TariffCategory && "Bedrag per deelnemer in deze categorie."}
                    {group.mode === PRICING_MODES.FixedPerParticipant && "Zelfde bedrag voor elke deelnemer."}
                    {group.mode === PRICING_MODES.ManualOption && "Speler kiest deze optie in het inschrijfformulier."}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => add(group.mode)}
                  className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-gray-200 text-xs text-gray-700 hover:bg-white"
                >
                  <Plus size={12} /> Optie
                </button>
              </div>

              {group.options.length === 0 ? (
                <p className="px-4 py-3 text-xs text-gray-400">Nog geen opties.</p>
              ) : (
                <div className="p-3 space-y-3">
                  {group.options.map((option) => (
                    <div
                      key={option.id}
                      className="rounded-lg border border-gray-200 bg-white p-3.5 space-y-3"
                    >
                      {/* Regel 1 — identiteit + prijs + verwijderen, alles enkelregelig en uitgelijnd */}
                      <div className="flex flex-wrap items-end gap-3">
                        <div className="flex-1 min-w-[180px]">
                          <label className="block text-xs font-medium text-gray-600 mb-1">Naam</label>
                          <input
                            className={inputClass}
                            value={option.label}
                            placeholder="bv. Jeugd, Duo, Sociaal tarief"
                            onChange={(e) => update(option.id, { label: e.target.value })}
                          />
                        </div>
                        {option.mode === PRICING_MODES.GroupSize && (
                          <div className="w-36">
                            <label className="block text-xs font-medium text-gray-600 mb-1">Groep</label>
                            <select
                              className={inputClass}
                              value={option.groupSize}
                              onChange={(e) => update(option.id, { groupSize: e.target.value })}
                            >
                              {GROUP_SIZES.map((size) => (
                                <option key={size} value={size}>{size === 1 ? "Privé (1)" : `${size} personen`}</option>
                              ))}
                            </select>
                          </div>
                        )}
                        {option.mode === PRICING_MODES.TariffCategory && (
                          <div className="w-40">
                            <label className="block text-xs font-medium text-gray-600 mb-1">Categorie</label>
                            <select
                              className={inputClass}
                              value={option.category}
                              onChange={(e) => update(option.id, { category: e.target.value })}
                            >
                              <option value="">Kies categorie</option>
                              {Object.values(PARTICIPANT_CATEGORIES).map((category) => (
                                <option key={category} value={category}>{CATEGORY_LABELS[category]}</option>
                              ))}
                            </select>
                          </div>
                        )}
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
                          placeholder="Wanneer geldt deze prijs?"
                          onChange={(e) => update(option.id, { description: e.target.value })}
                        />
                      </div>

                      {/* Regel 3 — geavanceerd/optioneel, gedempt, plus de samenvatting */}
                      <div className="flex flex-wrap items-end justify-between gap-3 pt-0.5">
                        <div className="w-full max-w-xs">
                          <label className="block text-[11px] font-medium text-gray-500 mb-1">
                            Herbruikbaar als <span className="font-normal text-gray-400">(optioneel)</span>
                          </label>
                          <input
                            className={inputClass}
                            value={option.reusableKey}
                            placeholder="bv. jeugd-tarief"
                            onChange={(e) => update(option.id, { reusableKey: e.target.value })}
                          />
                        </div>
                        <p className="text-[11px] text-gray-400 pb-2">{summary(option)}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>
          ))}

          {drafts.length === 0 && (
            <p className="text-xs text-gray-500">
              Geen prijsopties ingesteld. De reeks rekent nu nog <strong>€{legacyPrice} per deelnemer</strong>.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
