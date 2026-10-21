"use client";

import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { CheckCircle2, Euro } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  CampPricesEditor,
  campPricesSchema,
  type CampPricesFormValues,
} from "../new/_components/camp-prices-editor";
import {
  buildCampRequest,
  detailToDays,
  detailToPrices,
  toDateOnly,
} from "../new/_types";
import { updateCamp, type CampDetailDto } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";

interface CampPricesCardProps {
  camp: CampDetailDto;
  canEdit: boolean;
}

function CardHeader() {
  const t = useTranslations("camps");
  return (
    <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
      <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
        <Euro size={13} className="text-tennis-green" />
      </div>
      <div>
        <h2 className="text-sm font-semibold text-gray-900">
          {t("pricesTitle")}
        </h2>
        <p className="text-xs text-gray-400 mt-0.5">{t("pricesHint")}</p>
      </div>
    </div>
  );
}

function PriceRow({
  label,
  description,
  amount,
}: {
  label: string;
  description?: string | null;
  amount: number;
}) {
  return (
    <li className="flex items-start justify-between gap-4 rounded-lg border border-gray-100 px-4 py-3">
      <div className="min-w-0">
        <p className="text-sm font-medium text-gray-900">{label}</p>
        {description && (
          <p className="text-xs text-gray-500 mt-0.5">{description}</p>
        )}
      </div>
      <span className="shrink-0 text-sm font-semibold text-gray-900">
        €{amount}
      </span>
    </li>
  );
}

export function CampPricesCard({ camp, canEdit }: CampPricesCardProps) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [savedOk, setSavedOk] = useState(false);

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<CampPricesFormValues>({
    resolver: zodResolver(z.object({ prices: campPricesSchema })),
    defaultValues: { prices: detailToPrices(camp) },
  });

  // Na het opslaan komen de server-ids binnen: zet het formulier terug op de opgeslagen stand.
  useEffect(() => {
    reset({ prices: detailToPrices(camp) });
  }, [camp, reset]);

  const mutation = useMutation({
    mutationFn: (values: CampPricesFormValues) =>
      updateCamp(camp.id, {
        ...buildCampRequest(
          {
            name: camp.name,
            description: camp.description ?? "",
            tennisClubId: camp.tennisClubId,
            level: camp.level,
            price: camp.price,
            maxParticipants: camp.maxParticipants,
            startDate: toDateOnly(camp.startDate),
            endDate: toDateOnly(camp.endDate),
            registrationDeadline: toDateOnly(camp.registrationDeadline),
            prices: values.prices,
          },
          detailToDays(camp),
        ),
        isActive: camp.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camp", camp.id] });
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      setSavedOk(true);
      setTimeout(() => setSavedOk(false), 2500);
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
  });

  if (!canEdit) {
    if (camp.prices.length === 0) return null;
    return (
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
        <CardHeader />
        <ul className="p-6 space-y-2">
          <PriceRow label={t("publicPriceMain")} amount={camp.price} />
          {camp.prices.map((p) => (
            <PriceRow
              key={p.id}
              label={p.label}
              description={p.description}
              amount={p.amount}
            />
          ))}
        </ul>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit((values) => {
        setErrorMessages([]);
        setSavedOk(false);
        mutation.mutate(values);
      })}
      className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden"
    >
      <CardHeader />
      <div className="p-6 space-y-3">
        <PriceRow label={t("publicPriceMain")} amount={camp.price} />
        <CampPricesEditor
          register={register}
          control={control}
          errors={errors}
        />
      </div>
      <div className="border-t border-gray-100 px-6 py-3">
        {errorMessages.length > 0 && (
          <div className="mb-2 space-y-0.5 text-xs text-red-600">
            {errorMessages.map((msg, i) => (
              <p key={i}>{msg}</p>
            ))}
          </div>
        )}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p
            role="status"
            className={`inline-flex items-center gap-1.5 text-xs font-medium ${
              savedOk && !isDirty
                ? "text-tennis-green"
                : isDirty
                  ? "text-amber-700"
                  : "text-gray-400"
            }`}
          >
            {savedOk && !isDirty ? (
              <>
                <CheckCircle2 size={14} />
                {t("pricesSaved")}
              </>
            ) : isDirty ? (
              t("daysUnsaved")
            ) : (
              t("daysAllSaved")
            )}
          </p>
          {isDirty && (
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => reset({ prices: detailToPrices(camp) })}
                disabled={mutation.isPending}
                className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
              >
                {t("cancel")}
              </button>
              <button
                type="submit"
                disabled={mutation.isPending}
                className="rounded-lg bg-tennis-green px-5 py-2 text-sm font-semibold text-white hover:bg-tennis-green/90 disabled:opacity-60"
              >
                {mutation.isPending ? t("saving") : t("save")}
              </button>
            </div>
          )}
        </div>
      </div>
    </form>
  );
}
