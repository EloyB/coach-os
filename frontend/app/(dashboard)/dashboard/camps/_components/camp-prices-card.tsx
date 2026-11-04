"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Euro, Plus } from "lucide-react";
import { CampPriceDialog } from "./camp-price-dialog";
import {
  buildCampRequest,
  detailToDays,
  detailToPrices,
  toDateOnly,
  type CampPriceDraft,
} from "../new/_types";
import { updateCamp, type CampDetailDto } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";

interface CampPricesCardProps {
  camp: CampDetailDto;
  canEdit: boolean;
}

function PriceTile({
  label,
  description,
  amount,
  onClick,
}: {
  label: string;
  description?: string | null;
  amount: number;
  onClick?: () => void;
}) {
  const content = (
    <>
      <p className="text-lg font-semibold text-gray-900">€{amount}</p>
      <p className="mt-0.5 text-sm font-medium text-gray-700">{label}</p>
      {description && (
        <p className="mt-1 text-xs text-gray-500 line-clamp-2">{description}</p>
      )}
    </>
  );
  const base = "rounded-lg border border-gray-200 p-4 text-left";
  return onClick ? (
    <button
      type="button"
      onClick={onClick}
      className={`${base} transition-colors hover:border-tennis-green/50 hover:bg-gray-50`}
    >
      {content}
    </button>
  ) : (
    <div className={base}>{content}</div>
  );
}

export function CampPricesCard({ camp, canEdit }: CampPricesCardProps) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<CampPriceDraft | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);

  const mutation = useMutation({
    mutationFn: (prices: CampPriceDraft[]) =>
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
            prices,
          },
          detailToDays(camp),
        ),
        // Volledig tijdstip bewaren: buildCampRequest kapt de deadline af op middernacht.
        registrationDeadline: camp.registrationDeadline,
        isActive: camp.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camp", camp.id] });
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      setDialogOpen(false);
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
  });

  if (!canEdit && camp.prices.length === 0) return null;

  const current = detailToPrices(camp);

  function openDialog(price: CampPriceDraft | null) {
    setErrorMessages([]);
    setEditing(price);
    setDialogOpen(true);
  }

  function handleSubmit(price: CampPriceDraft) {
    setErrorMessages([]);
    mutation.mutate(
      editing
        ? current.map((p) => (p.id === editing.id ? { ...price, id: p.id } : p))
        : [...current, price],
    );
  }

  function handleDelete() {
    setErrorMessages([]);
    mutation.mutate(current.filter((p) => p.id !== editing?.id));
  }

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
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

      <div className="p-6 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <PriceTile label={t("publicPriceMain")} amount={camp.price} />
        {camp.prices.map((p, i) => (
          <PriceTile
            key={p.id}
            label={p.label}
            description={p.description}
            amount={p.amount}
            onClick={canEdit ? () => openDialog(current[i]) : undefined}
          />
        ))}
        {canEdit && camp.prices.length < 10 && (
          <button
            type="button"
            onClick={() => openDialog(null)}
            className="flex min-h-[5.5rem] items-center justify-center gap-1.5 rounded-lg border border-dashed border-gray-300 text-sm font-medium text-tennis-green transition-colors hover:border-tennis-green/50 hover:bg-gray-50"
          >
            <Plus size={14} />
            {t("priceOptionAdd")}
          </button>
        )}
      </div>

      {canEdit && (
        <CampPriceDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          price={editing}
          pending={mutation.isPending}
          errorMessages={errorMessages}
          onSubmit={handleSubmit}
          onDelete={handleDelete}
        />
      )}
    </div>
  );
}
