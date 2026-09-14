"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  Phone,
  MessageCircle,
  Copy,
  Check,
  RefreshCw,
  CheckCircle2,
  ChevronDown,
  MoreVertical,
  ExternalLink,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { getNonResponders } from "@/lib/api/confirmation";
import { resendConfirmation, adminConfirm } from "@/lib/api/planning";
import type { NonResponderDto } from "@/lib/api/confirmation";

// Backend-conventie: 0=maandag ... 6=zondag (zie CoachOS.Application/LessonSerie/LessonSerieService.cs).
const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

function formatRelativeExpiry(expiresAt: string): string {
  const now = new Date();
  const expires = new Date(expiresAt);
  const diffMs = expires.getTime() - now.getTime();

  if (diffMs <= 0) return "";

  const diffH = Math.floor(diffMs / (1000 * 60 * 60));
  const diffM = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

  if (diffH > 24) {
    const days = Math.floor(diffH / 24);
    return `${days}d ${diffH % 24}u`;
  }
  if (diffH > 0) return `${diffH}u ${diffM}m`;
  return `${diffM}m`;
}

export function NonRespondersPanel({
  seriesId,
  onOpenAssignment,
}: {
  seriesId: string;
  /** Klik op een rij → open de tijdslot-dialog van die toewijzing (o.a. voor Verplaatsen). */
  onOpenAssignment?: (assignmentId: string) => void;
}) {
  const t = useTranslations("nonResponders");
  const queryClient = useQueryClient();
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  // Uitklapbaar zoals de andere sidebar-secties; default open (actie vereist).
  const [open, setOpen] = useState(true);
  // Welke kaart heeft z'n 3-puntjes-menu open.
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  // Kaart waarvoor de 'manueel bevestigen'-bevestiging openstaat.
  const [confirmAdmin, setConfirmAdmin] = useState<NonResponderDto | null>(null);

  const { data: nonResponders = [] } = useQuery({
    queryKey: ["planning", seriesId, "non-responders"],
    queryFn: () => getNonResponders(seriesId),
    refetchInterval: 30000,
  });

  const resendMutation = useMutation({
    mutationFn: (assignmentId: string) =>
      resendConfirmation(seriesId, assignmentId),
    onSuccess: () => {
      showToast(t("resendSuccess"));
      queryClient.invalidateQueries({
        queryKey: ["planning", seriesId, "non-responders"],
      });
    },
    onError: () => {
      showToast(t("resendError"));
    },
  });

  const adminConfirmMutation = useMutation({
    mutationFn: (assignmentId: string) =>
      adminConfirm(seriesId, assignmentId),
    onSuccess: () => {
      showToast(t("adminConfirmSuccess"));
      queryClient.invalidateQueries({
        queryKey: ["planning", seriesId, "non-responders"],
      });
      queryClient.invalidateQueries({ queryKey: ["planning", seriesId] });
    },
  });

  function showToast(msg: string) {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  }

  function handleCopyEmail(nr: NonResponderDto) {
    navigator.clipboard.writeText(nr.studentEmail);
    setCopiedId(nr.assignmentId);
    setTimeout(() => setCopiedId(null), 2000);
  }

  if (nonResponders.length === 0) return null;

  return (
    <div className="p-4 border-b border-gray-100">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full cursor-pointer items-center justify-between"
      >
        <span className="flex items-center gap-2 text-sm font-semibold text-gray-900">
          {t("title")}
          <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700">
            {nonResponders.length}
          </span>
        </span>
        <ChevronDown
          size={16}
          className={`text-gray-400 transition-transform ${open ? "rotate-180" : ""}`}
        />
      </button>

      {/* Toast */}
      {toastMessage && (
        <div className="mt-3 px-3 py-2 rounded-lg bg-tennis-green/10 border border-tennis-green/20 text-xs text-tennis-green font-medium">
          {toastMessage}
        </div>
      )}

      {open && (
      <div className="mt-3 space-y-2">
        {nonResponders.map((nr) => (
          <div
            key={nr.assignmentId}
            className={`border rounded-lg p-3 ${
              nr.isExpired
                ? "border-red-200 bg-red-50/50"
                : "border-amber-200 bg-amber-50/50"
            }`}
          >
            {/* Data: naam + groep-badge + slot, met verloop-badge rechts. */}
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0">
                <div className="flex items-center gap-1.5">
                  <span className="truncate text-xs font-medium text-gray-900">
                    {nr.studentName}
                  </span>
                  {nr.isGroup && (
                    <span className="shrink-0 rounded bg-white/70 px-1.5 py-0.5 text-[10px] font-medium text-gray-500">
                      {t("groupBadge", { size: nr.groupSize })}
                    </span>
                  )}
                </div>
                <div className="mt-0.5 text-[10px] text-gray-500">
                  {DAY_NAMES_SHORT[nr.dayOfWeek]} {nr.startTime}
                  {nr.courtName && ` · ${nr.courtName}`}
                </div>
              </div>

              {nr.isExpired ? (
                <span className="shrink-0 rounded-full bg-red-100 px-2 py-0.5 text-[10px] font-medium text-red-700">
                  {t("expired")}
                </span>
              ) : (
                <span className="shrink-0 rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-medium text-amber-700">
                  {formatRelativeExpiry(nr.expiresAt)}
                </span>
              )}
            </div>

            {/* Acties: primaire 'opnieuw verzenden' + 3-puntjes-menu voor de rest. */}
            <div className="mt-2 flex items-center gap-2">
              <button
                type="button"
                disabled={resendMutation.isPending}
                onClick={() => resendMutation.mutate(nr.assignmentId)}
                className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-md border border-gray-200 bg-white px-2 py-1.5 text-[10px] font-medium text-gray-700 transition-colors hover:border-tennis-green/40 hover:text-tennis-green disabled:opacity-50"
              >
                <RefreshCw
                  size={11}
                  className={
                    resendMutation.isPending &&
                    resendMutation.variables === nr.assignmentId
                      ? "animate-spin"
                      : ""
                  }
                />
                {t("resend")}
              </button>

              <Popover
                open={openMenuId === nr.assignmentId}
                onOpenChange={(o) => setOpenMenuId(o ? nr.assignmentId : null)}
              >
                <PopoverTrigger asChild>
                  <button
                    type="button"
                    aria-label={t("actionsLabel", { name: nr.studentName })}
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-gray-200 bg-white text-gray-400 transition-colors hover:bg-gray-50 hover:text-gray-700"
                  >
                    <MoreVertical size={15} />
                  </button>
                </PopoverTrigger>
                <PopoverContent align="end" className="w-52 p-1 text-sm">
                  <button
                    type="button"
                    disabled={adminConfirmMutation.isPending}
                    onClick={() => {
                      setOpenMenuId(null);
                      setConfirmAdmin(nr);
                    }}
                    className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left font-medium text-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
                  >
                    <CheckCircle2 size={13} />
                    {t("adminConfirm")}
                  </button>

                  <div className="my-1 border-t border-gray-100" />

                  {nr.studentPhone && (
                    <>
                      <a
                        href={`tel:${nr.studentPhone}`}
                        className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-gray-700 hover:bg-gray-50"
                      >
                        <Phone size={13} />
                        {t("call")}
                      </a>
                      <a
                        href={`https://wa.me/${nr.studentPhone.replace(/[^0-9+]/g, "").replace(/^\+/, "")}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-gray-700 hover:bg-gray-50"
                      >
                        <MessageCircle size={13} />
                        {t("whatsapp")}
                      </a>
                    </>
                  )}
                  <button
                    type="button"
                    onClick={() => handleCopyEmail(nr)}
                    className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
                  >
                    {copiedId === nr.assignmentId ? (
                      <>
                        <Check size={13} className="text-green-500" />
                        {t("emailCopied")}
                      </>
                    ) : (
                      <>
                        <Copy size={13} />
                        {t("copyEmail")}
                      </>
                    )}
                  </button>

                  {onOpenAssignment && (
                    <>
                      <div className="my-1 border-t border-gray-100" />
                      <button
                        type="button"
                        onClick={() => {
                          setOpenMenuId(null);
                          onOpenAssignment(nr.assignmentId);
                        }}
                        className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
                      >
                        <ExternalLink size={13} />
                        {t("openAction")}
                      </button>
                    </>
                  )}
                </PopoverContent>
              </Popover>
            </div>
          </div>
        ))}
      </div>
      )}

      {/* Manueel bevestigen — bevestiging (één dialog, gevoed vanuit het 3-puntjes-menu). */}
      <AlertDialog
        open={confirmAdmin !== null}
        onOpenChange={(o) => !o && setConfirmAdmin(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("adminConfirmTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {confirmAdmin?.isGroup
                ? t("adminConfirmDescGroup", {
                    name: confirmAdmin.studentName,
                    size: confirmAdmin.groupSize,
                  })
                : t("adminConfirmDesc", { name: confirmAdmin?.studentName ?? "" })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annuleren</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (confirmAdmin) adminConfirmMutation.mutate(confirmAdmin.assignmentId);
                setConfirmAdmin(null);
              }}
              className="bg-tennis-green hover:bg-tennis-green/90"
            >
              {t("adminConfirmButton")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
