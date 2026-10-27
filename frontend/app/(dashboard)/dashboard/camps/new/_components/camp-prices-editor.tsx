"use client";

import { useTranslations } from "next-intl";
import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import {
  useFieldArray,
  type Control,
  type FieldErrors,
  type UseFormRegister,
} from "react-hook-form";
import * as z from "zod";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";

export const campPricesSchema = z
  .array(
    z.object({
      id: z.string().optional(),
      label: z
        .string()
        .trim()
        .min(1, "Naam is verplicht")
        .max(120, "Naam mag maximaal 120 karakters zijn"),
      description: z.string().max(500, "Uitleg is te lang"),
      discount: z
        .number({ message: "Korting is verplicht" })
        .min(0, "Korting mag niet negatief zijn")
        .max(100000, "Korting is onrealistisch hoog"),
    }),
  )
  .max(10, "Maximaal 10 prijsopties");

export type CampPricesFormValues = { prices: z.infer<typeof campPricesSchema> };

interface CampPricesEditorProps {
  register: UseFormRegister<CampPricesFormValues>;
  control: Control<CampPricesFormValues>;
  errors: FieldErrors<CampPricesFormValues>;
}

export function CampPricesEditor({
  register,
  control,
  errors,
}: CampPricesEditorProps) {
  const t = useTranslations("camps");
  const {
    fields: priceFields,
    append: appendPrice,
    remove: removePrice,
    move: movePrice,
  } = useFieldArray({ control, name: "prices" });

  return (
    <div>
      {priceFields.length > 0 && (
        <ul className="space-y-2.5 mb-2.5">
          {priceFields.map((field, index) => (
            <li
              key={field.id}
              className="rounded-lg border border-gray-200 p-3 space-y-2.5"
            >
              <div className="grid grid-cols-[minmax(0,1fr)_7.5rem] gap-3">
                <div>
                  <input
                    {...register(`prices.${index}.label`)}
                    type="text"
                    maxLength={120}
                    aria-label={t("priceOptionLabel")}
                    placeholder={t("priceOptionLabelPlaceholder")}
                    className={inputClass}
                  />
                  <FieldError
                    message={errors.prices?.[index]?.label?.message}
                  />
                </div>
                <div>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-gray-400 pointer-events-none">
                      €
                    </span>
                    <input
                      {...register(`prices.${index}.discount`, {
                        valueAsNumber: true,
                      })}
                      onFocus={(e) => e.currentTarget.select()}
                      type="number"
                      min={0}
                      step={0.01}
                      aria-label={t("priceOptionDiscount")}
                      className={inputClass + " pl-7"}
                    />
                  </div>
                  <FieldError
                    message={errors.prices?.[index]?.discount?.message}
                  />
                </div>
              </div>
              <div className="flex items-start gap-2">
                <div className="flex-1">
                  <input
                    {...register(`prices.${index}.description`)}
                    type="text"
                    maxLength={500}
                    aria-label={t("priceOptionDescription")}
                    placeholder={t("priceOptionDescriptionPlaceholder")}
                    className={inputClass}
                  />
                  <FieldError
                    message={errors.prices?.[index]?.description?.message}
                  />
                </div>
                <div className="flex shrink-0 items-center">
                  <button
                    type="button"
                    onClick={() => movePrice(index, index - 1)}
                    disabled={index === 0}
                    aria-label={t("priceOptionMoveUp")}
                    className="p-2 text-gray-400 hover:text-gray-700 disabled:opacity-30 disabled:hover:text-gray-400"
                  >
                    <ArrowUp size={15} />
                  </button>
                  <button
                    type="button"
                    onClick={() => movePrice(index, index + 1)}
                    disabled={index === priceFields.length - 1}
                    aria-label={t("priceOptionMoveDown")}
                    className="p-2 text-gray-400 hover:text-gray-700 disabled:opacity-30 disabled:hover:text-gray-400"
                  >
                    <ArrowDown size={15} />
                  </button>
                  <button
                    type="button"
                    onClick={() => removePrice(index)}
                    aria-label={t("priceOptionRemove")}
                    className="p-2 text-gray-400 hover:text-red-600"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
      {priceFields.length < 10 && (
        <button
          type="button"
          onClick={() => appendPrice({ label: "", description: "", discount: 0 })}
          className="inline-flex items-center gap-1.5 text-sm font-medium text-tennis-green hover:underline"
        >
          <Plus size={14} />
          {t("priceOptionAdd")}
        </button>
      )}
    </div>
  );
}
