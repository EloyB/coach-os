"use client";

import { useEffect, useRef } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { CreditCard, ExternalLink, Loader2 } from "lucide-react";
import { toast } from "sonner";
import {
  disconnectMollie,
  getMollieConnectionStatus,
  startMollieConnect,
} from "@/lib/api/mollieConnect";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

export function MollieSection() {
  const t = useTranslations("mollie");
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();

  const { data: status, isLoading } = useQuery({
    queryKey: ["mollieConnection"],
    queryFn: getMollieConnectionStatus,
  });

  // OAuth-callback redirect zet ?mollie=connected of ?mollie=error op de URL.
  // Toon één toast en strip de param zodat een refresh hem niet opnieuw triggert.
  // useRef + key-check voorkomt dubbele toasts in React strict-mode.
  const handledCallbackRef = useRef<string | null>(null);
  useEffect(() => {
    const value = searchParams.get("mollie");
    if (!value || handledCallbackRef.current === value) return;
    handledCallbackRef.current = value;

    if (value === "connected") {
      toast.success(t("toastConnected"));
      queryClient.invalidateQueries({ queryKey: ["mollieConnection"] });
      // Net Mollie verbonden tijdens onboarding — checklist meteen verversen.
      queryClient.invalidateQueries({ queryKey: ["onboarding"] });
    } else if (value === "error") {
      toast.error(t("toastError"));
    }

    // Strip de param zonder de history-entry te vervuilen.
    router.replace("/dashboard/settings", { scroll: false });
  }, [searchParams, router, t, queryClient]);

  const startMutation = useMutation({
    mutationFn: startMollieConnect,
    onSuccess: (response) => {
      // Full-page navigation naar Mollie zodat de OAuth-flow correct verloopt
      // (vermijdt CORS issues op het Mollie-domein).
      window.location.href = response.authorizationUrl;
    },
    onError: () => {
      toast.error(t("startError"));
    },
  });

  const disconnectMutation = useMutation({
    mutationFn: disconnectMollie,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mollieConnection"] });
      toast.success(t("toastDisconnected"));
    },
    onError: () => {
      toast.error(t("disconnectError"));
    },
  });

  const isConnected = status?.connected ?? false;
  const connectedAtLabel = formatConnectedAt(status?.connectedAt ?? null);

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden mb-6">
      {/* Section header */}
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-7 h-7 rounded-lg bg-tennis-green/10 flex items-center justify-center">
          <CreditCard size={14} className="text-tennis-green" />
        </div>
        <div>
          <h2 className="text-sm font-semibold text-gray-800">
            {t("sectionTitle")}
          </h2>
          <p className="text-xs text-gray-400">{t("sectionSubtitle")}</p>
        </div>
      </div>

      <div className="p-5">
        {isLoading && (
          <div className="flex items-center gap-2 text-xs text-gray-400">
            <Loader2 size={14} className="animate-spin" />
            <span>Laden...</span>
          </div>
        )}

        {!isLoading && !isConnected && (
          <NotConnectedView
            onConnect={() => startMutation.mutate()}
            isPending={startMutation.isPending}
          />
        )}

        {!isLoading && isConnected && status && (
          <ConnectedView
            organizationName={status.mollieOrganizationName ?? "Mollie"}
            connectedAtLabel={connectedAtLabel}
            onDisconnect={() => disconnectMutation.mutate()}
            isPending={disconnectMutation.isPending}
          />
        )}
      </div>
    </div>
  );
}

// ─── Subviews ────────────────────────────────────────────────────────────────

interface NotConnectedViewProps {
  onConnect: () => void;
  isPending: boolean;
}

function NotConnectedView({ onConnect, isPending }: NotConnectedViewProps) {
  const t = useTranslations("mollie");
  return (
    <div className="space-y-4">
      <div>
        <p className="text-sm font-semibold text-gray-800">
          {t("notConnectedTitle")}
        </p>
        <p className="text-xs text-gray-500 mt-1 leading-relaxed">
          {t("notConnectedBody")}
        </p>
      </div>

      <button
        type="button"
        onClick={onConnect}
        disabled={isPending}
        className="inline-flex items-center gap-2 px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
      >
        {isPending ? (
          <Loader2 size={13} className="animate-spin" />
        ) : (
          <CreditCard size={13} />
        )}
        {isPending ? t("connecting") : t("connectButton")}
      </button>

      <p className="text-[11px] text-gray-400 pt-2 border-t border-gray-100">
        {t("noAccountHint")}{" "}
        <a
          href="https://www.mollie.com/dashboard/signup"
          target="_blank"
          rel="noreferrer noopener"
          className="inline-flex items-center gap-1 text-tennis-green font-medium hover:underline"
        >
          mollie.com
          <ExternalLink size={10} />
        </a>
      </p>
    </div>
  );
}

interface ConnectedViewProps {
  organizationName: string;
  connectedAtLabel: string | null;
  onDisconnect: () => void;
  isPending: boolean;
}

function ConnectedView({
  organizationName,
  connectedAtLabel,
  onDisconnect,
  isPending,
}: ConnectedViewProps) {
  const t = useTranslations("mollie");
  return (
    <div className="space-y-4">
      <div>
        <div className="flex items-center gap-2">
          <span className="inline-block w-2 h-2 rounded-full bg-emerald-500" />
          <p className="text-sm font-semibold text-gray-800">
            {t("connectedTitle", { name: organizationName })}
          </p>
        </div>
        {connectedAtLabel && (
          <p className="text-xs text-gray-400 mt-1">
            {t("connectedSince", { date: connectedAtLabel })}
          </p>
        )}
        <p className="text-xs text-gray-500 mt-2 leading-relaxed">
          {t("connectedBody")}
        </p>
      </div>

      <AlertDialog>
        <AlertDialogTrigger asChild>
          <button
            type="button"
            disabled={isPending}
            className="inline-flex items-center gap-2 px-4 py-2 bg-white border border-gray-200 text-gray-700 text-xs font-semibold rounded-lg hover:bg-gray-50 hover:border-gray-300 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isPending && <Loader2 size={13} className="animate-spin" />}
            {isPending ? t("disconnecting") : t("disconnectButton")}
          </button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("disconnectConfirmTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("disconnectConfirmBody")}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("disconnectCancel")}</AlertDialogCancel>
            <AlertDialogAction
              onClick={onDisconnect}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              {t("disconnectConfirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function formatConnectedAt(iso: string | null): string | null {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;
  // Project gebruikt dd/MM/yyyy als datumconventie (zie CLAUDE.md).
  return new Intl.DateTimeFormat("nl-BE", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(date);
}
