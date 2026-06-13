"use client";

import { useEffect, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  ChevronUp,
  ChevronDown,
  Trash2,
  Plus,
  X,
  ClipboardList,
} from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import {
  getCampForm,
  saveCampForm,
  type SaveCampFormFieldRequest,
} from "@/lib/api/camps";

const FIELD_TYPES = [
  { value: 1, label: "Vrije tekst" },
  { value: 2, label: "Meerkeuze" },
  { value: 3, label: "Ja/Nee" },
];

interface DraftField extends SaveCampFormFieldRequest {
  _key: string;
}

export function CampFormBuilder({ campId }: { campId: string }) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();
  const [fields, setFields] = useState<DraftField[]>([]);
  const [loaded, setLoaded] = useState(false);

  const { data: existingForm } = useQuery({
    queryKey: ["campForm", campId],
    queryFn: () => getCampForm(campId),
  });

  // Initialise draft fields from loaded form
  useEffect(() => {
    if (!loaded && existingForm !== undefined) {
      if (existingForm) {
        setFields(
          existingForm.fields.map((f) => ({
            _key: f.id,
            id: f.id,
            label: f.label,
            type: f.type,
            isRequired: f.isRequired,
            order: f.order,
            options: f.options ?? undefined,
          })),
        );
      }
      setLoaded(true);
    }
  }, [existingForm, loaded]);

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload: SaveCampFormFieldRequest[] = fields.map((f, i) => ({
        id: f.id,
        label: f.label,
        type: f.type,
        isRequired: f.isRequired,
        order: i,
        options: f.type === 2 ? f.options : undefined,
      }));
      return saveCampForm(campId, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["campForm", campId] });
    },
  });

  function addField() {
    setFields((prev) => [
      ...prev,
      {
        _key: Math.random().toString(36).slice(2),
        label: "",
        type: 1,
        isRequired: false,
        order: prev.length,
      },
    ]);
  }

  function updateField(key: string, updates: Partial<DraftField>) {
    setFields((prev) =>
      prev.map((f) => (f._key === key ? { ...f, ...updates } : f)),
    );
  }

  function removeField(key: string) {
    setFields((prev) => prev.filter((f) => f._key !== key));
  }

  function moveField(key: string, direction: -1 | 1) {
    setFields((prev) => {
      const idx = prev.findIndex((f) => f._key === key);
      if (idx < 0) return prev;
      const newIdx = idx + direction;
      if (newIdx < 0 || newIdx >= prev.length) return prev;
      const next = [...prev];
      [next[idx], next[newIdx]] = [next[newIdx], next[idx]];
      return next;
    });
  }

  function addOption(key: string) {
    setFields((prev) =>
      prev.map((f) =>
        f._key === key ? { ...f, options: [...(f.options ?? []), ""] } : f,
      ),
    );
  }

  function updateOption(key: string, optIdx: number, value: string) {
    setFields((prev) =>
      prev.map((f) => {
        if (f._key !== key) return f;
        const opts = [...(f.options ?? [])];
        opts[optIdx] = value;
        return { ...f, options: opts };
      }),
    );
  }

  function removeOption(key: string, optIdx: number) {
    setFields((prev) =>
      prev.map((f) => {
        if (f._key !== key) return f;
        const opts = (f.options ?? []).filter((_, i) => i !== optIdx);
        return { ...f, options: opts };
      }),
    );
  }

  const inputCls =
    "w-full px-3 py-2 text-sm border border-rule rounded-lg focus:outline-none focus:border-tennis-green bg-white";

  return (
    <div className="bg-paper border border-rule rounded-xl overflow-hidden">
      <div className="px-5 py-4 border-b border-rule flex items-center gap-2.5">
        <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
          <ClipboardList size={13} className="text-tennis-green" />
        </div>
        <div>
          <h2 className="text-sm font-semibold text-ink">
            {t("formBuilderTitle")}
          </h2>
          <p className="text-[11px] text-ink-3 mt-0.5">
            {t("formBuilderDescription")}
          </p>
        </div>
      </div>

      <div className="p-5 space-y-4">
        {/* Predefined fields badge list */}
        <div>
          <p className="text-xs text-ink-3 mb-2">{t("formFixedFields")}</p>
          <div className="flex flex-wrap gap-2">
            {[t("formFirstName"), t("formLastName"), t("formEmail"), t("formPhone")].map(
              (f) => (
                <span
                  key={f}
                  className="inline-flex items-center px-2.5 py-1 rounded-full text-xs bg-tennis-green/10 text-tennis-green font-medium"
                >
                  {f}
                </span>
              ),
            )}
          </div>
        </div>

        {/* Custom fields */}
        {fields.length === 0 && (
          <p className="text-xs text-ink-3 py-2">{t("formNoCustomFields")}</p>
        )}

        <div className="space-y-3">
          {fields.map((field, idx) => (
            <div
              key={field._key}
              className="border border-rule rounded-xl p-4 space-y-3 bg-canvas"
            >
              {/* Row 1: label (full width) */}
              <input
                type="text"
                placeholder={t("formFieldLabelPlaceholder")}
                value={field.label}
                onChange={(e) =>
                  updateField(field._key, { label: e.target.value })
                }
                className={inputCls}
              />

              {/* Row 2: type + required + reorder + delete */}
              <div className="flex items-center gap-2">
                <NativeSelect
                  value={field.type}
                  onChange={(e) =>
                    updateField(field._key, {
                      type: parseInt(e.target.value, 10),
                      options: undefined,
                    })
                  }
                >
                  {FIELD_TYPES.map((ft) => (
                    <option key={ft.value} value={ft.value}>
                      {ft.label}
                    </option>
                  ))}
                </NativeSelect>

                <label className="flex items-center gap-1.5 text-xs text-ink-2 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    checked={field.isRequired}
                    onChange={(e) =>
                      updateField(field._key, { isRequired: e.target.checked })
                    }
                    className="accent-tennis-green"
                  />
                  {t("formFieldRequired")}
                </label>

                <div className="flex items-center gap-1 ml-auto">
                  <button
                    type="button"
                    onClick={() => moveField(field._key, -1)}
                    disabled={idx === 0}
                    className="w-7 h-7 flex items-center justify-center rounded-lg text-ink-3 hover:text-ink-2 hover:bg-canvas disabled:opacity-30 transition-colors"
                  >
                    <ChevronUp size={13} />
                  </button>
                  <button
                    type="button"
                    onClick={() => moveField(field._key, 1)}
                    disabled={idx === fields.length - 1}
                    className="w-7 h-7 flex items-center justify-center rounded-lg text-ink-3 hover:text-ink-2 hover:bg-canvas disabled:opacity-30 transition-colors"
                  >
                    <ChevronDown size={13} />
                  </button>
                  <button
                    type="button"
                    onClick={() => removeField(field._key)}
                    className="w-7 h-7 flex items-center justify-center rounded-lg text-ink-3 hover:text-red-500 hover:bg-red-50 transition-colors"
                  >
                    <Trash2 size={13} />
                  </button>
                </div>
              </div>

              {/* Options for MultipleChoice */}
              {field.type === 2 && (
                <div className="space-y-2 pt-1 border-t border-rule">
                  {(field.options ?? []).map((opt, optIdx) => (
                    <div key={optIdx} className="flex items-center gap-2">
                      <input
                        type="text"
                        placeholder={t("formOptionPlaceholder", {
                          index: optIdx + 1,
                        })}
                        value={opt}
                        onChange={(e) =>
                          updateOption(field._key, optIdx, e.target.value)
                        }
                        className={inputCls + " flex-1"}
                      />
                      <button
                        type="button"
                        onClick={() => removeOption(field._key, optIdx)}
                        className="w-7 h-7 flex items-center justify-center rounded-lg text-ink-3 hover:text-red-500 hover:bg-red-50 transition-colors"
                      >
                        <X size={13} />
                      </button>
                    </div>
                  ))}
                  <button
                    type="button"
                    onClick={() => addOption(field._key)}
                    className="flex items-center gap-1 text-xs text-tennis-green hover:underline"
                  >
                    <Plus size={11} />
                    {t("formAddOption")}
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>

        <div className="flex items-center gap-3 pt-1">
          <button
            type="button"
            onClick={addField}
            className="flex items-center gap-1.5 px-3 py-2 border border-rule text-xs font-medium text-ink-2 rounded-lg hover:bg-canvas transition-colors"
          >
            <Plus size={12} />
            {t("formAddField")}
          </button>
          <button
            type="button"
            onClick={() => saveMutation.mutate()}
            disabled={saveMutation.isPending}
            className="flex items-center gap-1.5 px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
          >
            {saveMutation.isPending
              ? t("saving")
              : saveMutation.isSuccess
                ? t("formSaved")
                : t("formSave")}
          </button>
        </div>

        {saveMutation.isError && (
          <p className="text-xs text-red-500">{t("saveError")}</p>
        )}
      </div>
    </div>
  );
}
