"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { NativeSelect } from "@/components/ui/native-select";
import { btnSecondary, inputClass } from "@/lib/styles";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import {
  erasePlayer,
  getPlayerErasurePreview,
  type ErasureKind,
  type PlayerErasurePreview,
} from "@/lib/api/privacy";

interface PlayerErasureDialogProps {
  kind: ErasureKind;
  enrollmentId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Na een geslaagde verwijdering (bv. om het detailvenster te sluiten). */
  onErased: () => void;
}

const btnDanger =
  "px-4 py-2 rounded-lg text-sm font-semibold text-white bg-red-600 hover:bg-red-700 transition-colors disabled:opacity-60 disabled:cursor-not-allowed";

/**
 * "Speler verwijderen": toont eerst het overzicht (wat verdwijnt / waarom geblokkeerd) en
 * vraagt de exacte spelersnaam. De server controleert alles opnieuw binnen de transactie.
 */
export function PlayerErasureDialog({
  kind,
  enrollmentId,
  open,
  onOpenChange,
  onErased,
}: PlayerErasureDialogProps) {
  const t = useTranslations("playerErasure");
  const preview = useQuery({
    queryKey: ["playerErasurePreview", kind, enrollmentId],
    queryFn: () => getPlayerErasurePreview(kind, enrollmentId),
    enabled: open,
    staleTime: 0,
    gcTime: 0,
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onClick={(e) => e.stopPropagation()} className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("title")}</DialogTitle>
          <DialogDescription>{t("description")}</DialogDescription>
        </DialogHeader>
        {preview.isLoading && <p className="text-sm text-gray-400">{t("loading")}</p>}
        {preview.isError && (
          <p role="alert" className="text-sm text-red-600">
            {getAxiosErrorMessages(preview.error, t("previewError")).join(" ")}
          </p>
        )}
        {preview.data && (
          <ErasureBody
            kind={kind}
            preview={preview.data}
            onCancel={() => onOpenChange(false)}
            onErased={onErased}
            onStale={() => preview.refetch()}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}

interface ErasureBodyProps {
  kind: ErasureKind;
  preview: PlayerErasurePreview;
  onCancel: () => void;
  onErased: () => void;
  onStale: () => void;
}

function ErasureBody({ kind, preview, onCancel, onErased, onStale }: ErasureBodyProps) {
  const t = useTranslations("playerErasure");
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [leaderId, setLeaderId] = useState("");
  const needsLeader = preview.groupEffect === "TransferLeader";

  const mutation = useMutation({
    mutationFn: () =>
      erasePlayer(kind, preview.enrollmentId, {
        confirmationName: name,
        newGroupLeaderId: needsLeader ? leaderId : null,
        expectedUpdatedAt: preview.updatedAt,
      }),
    onSuccess: () => {
      toast.success(t("done"));
      queryClient.invalidateQueries();
      onErased();
    },
    // De foutmelding zelf toont de globale API-interceptor al als toast; hier alleen het
    // overzicht vernieuwen zodat nieuwe blokkades meteen zichtbaar worden.
    onError: () => onStale(),
  });

  const confirmed = name.trim() === preview.participantName.trim();
  const canSubmit = preview.canErase && confirmed && (!needsLeader || leaderId !== "") && !mutation.isPending;

  return (
    <div className="flex flex-col gap-4">
      <ErasureSummary preview={preview} />
      {preview.canErase ? (
        <>
          {needsLeader && (
            <div className="space-y-1.5">
              <label htmlFor="erasure-new-leader" className="text-sm font-medium text-gray-700">
                {t("newLeader")}
              </label>
              <NativeSelect id="erasure-new-leader" value={leaderId} onChange={(e) => setLeaderId(e.target.value)}>
                <option value="">{t("chooseLeader")}</option>
                {preview.otherGroupMembers.map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </NativeSelect>
            </div>
          )}
          <div className="space-y-1.5">
            <label htmlFor="erasure-confirm-name" className="text-sm font-medium text-gray-700">
              {t("confirmLabel", { name: preview.participantName })}
            </label>
            <input
              id="erasure-confirm-name"
              className={inputClass}
              autoComplete="off"
              maxLength={250}
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </div>
        </>
      ) : (
        <BlockingReasons reasons={preview.blockingReasons} />
      )}
      <div className="flex justify-end gap-2">
        <button type="button" className={btnSecondary} onClick={onCancel}>
          {t("cancel")}
        </button>
        {preview.canErase && (
          <button type="button" className={btnDanger} disabled={!canSubmit} onClick={() => mutation.mutate()}>
            {mutation.isPending ? t("busy") : t("confirm")}
          </button>
        )}
      </div>
    </div>
  );
}

function ErasureSummary({ preview }: { preview: PlayerErasurePreview }) {
  const t = useTranslations("playerErasure");
  return (
    <div className="space-y-2 rounded-lg bg-[#FAFAF8] p-3 text-sm text-gray-700">
      <p className="font-medium text-gray-900">{t("willDelete")}</p>
      <ul className="list-inside list-disc space-y-0.5">
        <li>{t("deleteEnrollment")}</li>
        <li>{t("deleteAnswers", { count: preview.formAnswerCount })}</li>
        <li>{t("deleteRelated")}</li>
        {preview.groupEffect !== "None" && <li>{t(`group.${preview.groupEffect}`)}</li>}
      </ul>
      <p className="text-xs text-gray-500">{t("keeps")}</p>
      {preview.lastParticipation && (
        <p className="text-xs text-gray-500">
          {t("lastParticipation", { date: new Date(preview.lastParticipation).toLocaleDateString("nl-BE") })}
          {" · "}
          {preview.retentionPeriodPassed ? t("retentionPassed") : t("retentionNotPassed")}
        </p>
      )}
    </div>
  );
}

function BlockingReasons({ reasons }: { reasons: string[] }) {
  const t = useTranslations("playerErasure");
  return (
    <div role="alert" className="space-y-1.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
      <p className="font-medium">{t("blockedTitle")}</p>
      <ul className="list-inside list-disc space-y-0.5">
        {reasons.map((reason) => (
          <li key={reason}>{t.has(`reasons.${reason}`) ? t(`reasons.${reason}`) : t("reasons.unknown")}</li>
        ))}
      </ul>
    </div>
  );
}
