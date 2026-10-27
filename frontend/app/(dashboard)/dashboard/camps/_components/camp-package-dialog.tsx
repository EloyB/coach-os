"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { btnPrimary, btnSecondary, inputClass } from "@/lib/styles";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { formatDayHeading, type CampDayDraft, type CampPackageDraft } from "../new/_types";

const EMPTY: CampPackageDraft = { name: "", description: "", price: 0, slots: [] };

const slotKey = (date: string, partOrder: number) => `${date}#${partOrder}`;

interface CampPackageDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Dagen met dagdelen; bepalen het raster waaruit gekozen wordt. */
  days: CampDayDraft[];
  /** Bestaand pakket om te bewerken, of null om een nieuw aan te maken. */
  pkg: CampPackageDraft | null;
  pending: boolean;
  errorMessages: string[];
  onSubmit: (pkg: CampPackageDraft) => void;
  onDelete?: () => void;
}

/** Dialog om een pakket te maken of aan te passen: naam, prijs en een raster van dagen × dagdelen. */
export function CampPackageDialog(props: CampPackageDialogProps) {
  // Opnieuw mounten bij openen: zo start het formulier altijd met de waarden van het gekozen pakket.
  return props.open ? <PackageForm {...props} /> : null;
}

function PackageForm({
  onOpenChange,
  days,
  pkg,
  pending,
  errorMessages,
  onSubmit,
  onDelete,
}: CampPackageDialogProps) {
  const t = useTranslations("camps");
  const initial = pkg ?? EMPTY;
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [price, setPrice] = useState(Number.isNaN(initial.price) ? "" : String(initial.price));
  const [selected, setSelected] = useState(
    () => new Set(initial.slots.map((s) => slotKey(s.date, s.partOrder))),
  );
  const [submitted, setSubmitted] = useState(false);

  const priceValue = price === "" ? NaN : Number(price);
  const nameError = !name.trim();
  const priceError = Number.isNaN(priceValue) || priceValue < 0;
  const slotsError = selected.size === 0;

  function toggle(date: string, partOrder: number) {
    const key = slotKey(date, partOrder);
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitted(true);
    if (nameError || priceError || slotsError) return;
    onSubmit({
      id: pkg?.id,
      name: name.trim(),
      description: description.trim(),
      price: priceValue,
      slots: days.flatMap((d) =>
        d.parts
          .map((_, order) => ({ date: d.date, partOrder: order }))
          .filter((s) => selected.has(slotKey(s.date, s.partOrder))),
      ),
    });
  }

  return (
    <Dialog open onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[85vh] flex-col gap-0 overflow-hidden p-0 sm:max-w-xl">
        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          <div className="flex-1 space-y-4 overflow-y-auto px-6 pb-4 pt-6">
            <DialogHeader>
              <DialogTitle>
                {pkg ? t("packageDialogEditTitle") : t("packageDialogNewTitle")}
              </DialogTitle>
              <DialogDescription>{t("packageDialogDescription")}</DialogDescription>
            </DialogHeader>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-[minmax(0,1fr)_10rem]">
              <div>
                <label className="mb-1.5 block text-sm font-medium text-gray-700">
                  {t("packageName")}
                </label>
                <input
                  type="text"
                  value={name}
                  maxLength={120}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t("packageNamePlaceholder")}
                  className={inputClass}
                />
                {submitted && nameError && (
                  <p className="mt-1 text-xs text-red-600">{t("nameRequired")}</p>
                )}
              </div>
              <div>
                <label className="mb-1.5 block text-sm font-medium text-gray-700">
                  {t("packagePrice")}
                </label>
                <div className="relative">
                  <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-gray-400">
                    €
                  </span>
                  <input
                    type="number"
                    min={0}
                    step={0.01}
                    value={price}
                    onFocus={(e) => e.currentTarget.select()}
                    onChange={(e) => setPrice(e.target.value)}
                    className={inputClass + " pl-7"}
                  />
                </div>
                {submitted && priceError && (
                  <p className="mt-1 text-xs text-red-600">{t("priceInvalid")}</p>
                )}
              </div>
            </div>

            <div>
              <label className="mb-1.5 block text-sm font-medium text-gray-700">
                {t("packageDescription")}
              </label>
              <input
                type="text"
                value={description}
                maxLength={500}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t("packageDescriptionPlaceholder")}
                className={inputClass}
              />
            </div>

            <div>
              <p className="mb-2 text-sm font-medium text-gray-700">{t("packageSlots")}</p>
              <div className="space-y-1.5">
                {days.map((day) => (
                  <div
                    key={day.date}
                    className="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg border border-gray-200 px-3 py-2"
                  >
                    <span className="w-40 shrink-0 text-[13px] font-medium text-gray-700 first-letter:uppercase">
                      {formatDayHeading(day.date)}
                    </span>
                    {day.parts.length === 0 ? (
                      <span className="text-xs text-gray-400">{t("packageNoParts")}</span>
                    ) : (
                      day.parts.map((part, order) => (
                        <label
                          key={order}
                          className="inline-flex cursor-pointer items-center gap-2 text-[13px] text-gray-700"
                        >
                          <input
                            type="checkbox"
                            checked={selected.has(slotKey(day.date, order))}
                            onChange={() => toggle(day.date, order)}
                            className="h-4 w-4 rounded border-gray-300 accent-tennis-green"
                          />
                          {part.name} {part.startTime}-{part.endTime}
                        </label>
                      ))
                    )}
                  </div>
                ))}
              </div>
              {submitted && slotsError && (
                <p className="mt-1 text-xs text-red-600">{t("packageSlotsRequired")}</p>
              )}
            </div>

            {errorMessages.length > 0 && (
              <div className="space-y-0.5 text-xs text-red-600">
                {errorMessages.map((msg, i) => (
                  <p key={i}>{msg}</p>
                ))}
              </div>
            )}
          </div>

          <DialogFooter className="border-t border-gray-100 px-6 py-4 sm:justify-between">
            {pkg && onDelete ? (
              <button
                type="button"
                onClick={onDelete}
                disabled={pending}
                className="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
              >
                {t("packageRemove")}
              </button>
            ) : (
              <span />
            )}
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => onOpenChange(false)}
                disabled={pending}
                className={btnSecondary}
              >
                {t("cancel")}
              </button>
              <button type="submit" disabled={pending} className={btnPrimary}>
                {pending ? t("saving") : t("save")}
              </button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
