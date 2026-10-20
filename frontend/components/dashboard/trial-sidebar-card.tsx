"use client";

import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getSubscriptionStatus } from "@/lib/api/subscription";
import type { AuthUser } from "@/lib/auth";
import { subscriptionStatusQueryKey } from "@/lib/subscription-query";
import { cn } from "@/lib/utils";

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-");
  return `${day}/${month}/${year}`;
}

/**
 * Kleine kaart in de sidenav met de status van de gratis proef (resterende dagen of afgelopen).
 * Enkel voor admins in een lopende proef; bij alleen-lezen toont de brede banner bovenaan de melding.
 */
export function TrialSidebarCard({ user }: { user: AuthUser | null }) {
  const t = useTranslations("subscription");
  const isAdmin = user?.role === "Admin";

  const { data: subscription } = useQuery({
    queryKey: subscriptionStatusQueryKey(user?.organizationId),
    queryFn: getSubscriptionStatus,
    enabled: isAdmin,
    staleTime: 5 * 60_000,
    retry: false,
  });

  if (
    !isAdmin ||
    subscription?.status !== "Trialing" ||
    !subscription.trialEndsAt ||
    subscription.accessLevel === "ReadOnly"
  ) {
    return null;
  }

  const expired = subscription.isTrialExpired;
  const days = subscription.trialDaysRemaining ?? 0;

  return (
    <div
      role="status"
      className={cn(
        "relative z-10 mx-2.5 mb-2.5 rounded-lg px-3 py-2.5",
        expired ? "bg-amber-400/20" : "bg-black/[.18]",
      )}
    >
      <p className="text-[9.5px] font-bold uppercase tracking-[0.12em] text-white/50">
        {t("trialCardLabel")}
      </p>
      <p
        className={cn(
          "mt-1 text-[12px] font-semibold",
          expired ? "text-amber-200" : "text-white",
        )}
      >
        {expired ? t("trialCardExpired") : t("trialCardDays", { days })}
      </p>
      <p className="mt-0.5 text-[10.5px] leading-snug text-white/55">
        {expired
          ? t("trialCardExpiredHint")
          : t("trialCardUntil", { date: formatDate(subscription.trialEndsAt) })}
      </p>
    </div>
  );
}
