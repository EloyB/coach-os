"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { campPricesSchema } from "../new/_components/camp-prices-editor";
import type { CampPriceDraft } from "../new/_types";
import { FieldError } from "@/components/forms/field-error";
import { btnPrimary, btnSecondary, inputClass } from "@/lib/styles";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

const priceSchema = campPricesSchema.element;

const EMPTY: CampPriceDraft = { label: "", description: "", amount: 0 };

interface CampPriceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Bestaande optie om te bewerken, of null om een nieuwe aan te maken. */
  price: CampPriceDraft | null;
  pending: boolean;
  errorMessages: string[];
  onSubmit: (price: CampPriceDraft) => void;
  onDelete?: () => void;
}

export function CampPriceDialog({
  open,
  onOpenChange,
  price,
  pending,
  errorMessages,
  onSubmit,
  onDelete,
}: CampPriceDialogProps) {
  const t = useTranslations("camps");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CampPriceDraft>({
    resolver: zodResolver(priceSchema),
    defaultValues: price ?? EMPTY,
  });

  useEffect(() => {
    if (open) reset(price ?? EMPTY);
  }, [open, price, reset]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {price ? t("priceDialogEditTitle") : t("priceDialogNewTitle")}
          </DialogTitle>
          <DialogDescription>{t("priceDialogDescription")}</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-[minmax(0,1fr)_7.5rem] gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("priceOptionLabel")}
              </label>
              <input
                {...register("label")}
                type="text"
                maxLength={120}
                placeholder={t("priceOptionLabelPlaceholder")}
                className={inputClass}
              />
              <FieldError message={errors.label?.message} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("priceOptionAmount")}
              </label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-gray-400 pointer-events-none">
                  €
                </span>
                <input
                  {...register("amount", { valueAsNumber: true })}
                  onFocus={(e) => e.currentTarget.select()}
                  type="number"
                  min={0}
                  step={0.01}
                  className={inputClass + " pl-7"}
                />
              </div>
              <FieldError message={errors.amount?.message} />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              {t("priceOptionDescription")}
            </label>
            <input
              {...register("description")}
              type="text"
              maxLength={500}
              placeholder={t("priceOptionDescriptionPlaceholder")}
              className={inputClass}
            />
            <FieldError message={errors.description?.message} />
          </div>

          {errorMessages.length > 0 && (
            <div className="space-y-0.5 text-xs text-red-600">
              {errorMessages.map((msg, i) => (
                <p key={i}>{msg}</p>
              ))}
            </div>
          )}

          <DialogFooter className="sm:justify-between">
            {price && onDelete ? (
              <button
                type="button"
                onClick={onDelete}
                disabled={pending}
                className="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
              >
                {t("priceOptionRemove")}
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
