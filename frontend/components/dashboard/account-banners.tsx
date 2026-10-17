"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import { getSubscriptionStatus } from "@/lib/api/subscription";
import type { AuthUser } from "@/lib/auth";
import { subscriptionStatusQueryKey } from "@/lib/subscription-query";

type Tone = "info" | "warn";

interface Banner {
  key: string;
  tone: Tone;
  chip: string;
  body: string;
  action?: { href: string; label: string };
}

const TONE_STYLES: Record<Tone, { bar: string; chip: string }> = {
  info: { bar: "border-l-tennis-green", chip: "bg-tennis-green/10 text-tennis-green" },
  warn: { bar: "border-l-amber-400", chip: "bg-amber-100 text-amber-800" },
};

const SUBSCRIPTION_PAGE = "/dashboard/settings/subscription";

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-");
  return `${day}/${month}/${year}`;
}

interface AccountBannersProps {
  user: AuthUser;
}

/**
 * Smalle meldingen onder de topbar over de staat van het account: lopende of afgelopen
 * proef en een nog niet bevestigd e-mailadres. Bestaande klanten (Legacy) en betalende
 * abonnementen zien hier niets.
 */
export function AccountBanners({ user }: AccountBannersProps) {
  const t = useTranslations("subscription");
  const isAdmin = user.role === "Admin";

  const { data: subscription } = useQuery({
    queryKey: subscriptionStatusQueryKey(user.organizationId),
    queryFn: getSubscriptionStatus,
    staleTime: 5 * 60_000,
    retry: false,
  });

  const banners: Banner[] = [];

  if (user.emailVerified === false) {
    banners.push({
      key: "verify",
      tone: "warn",
      chip: t("verifyChip"),
      body: t("verifyBody"),
      action: { href: "/verify-email", label: t("verifyCta") },
    });
  }

  const choosePlan = { href: SUBSCRIPTION_PAGE, label: t("choosePlanCta") };

  if (subscription?.accessLevel === "ReadOnly") {
    // Trainers kunnen niets aan het abonnement doen: neutrale melding zonder details.
    const locked = subscription.status === "Locked";
    banners.push({
      key: "read-only",
      tone: "warn",
      chip: locked ? t("lockedChip") : t("readOnlyChip"),
      body: isAdmin ? (locked ? t("lockedAdminBody") : t("readOnlyAdminBody")) : t("readOnlyTrainerBody"),
      action: isAdmin ? choosePlan : undefined,
    });
  } else if (isAdmin && subscription?.status === "Cancelled" && subscription.cancelAt) {
    banners.push({
      key: "cancelled",
      tone: "info",
      chip: t("cancelledChip"),
      body: t("cancelledBody", { date: formatDate(subscription.cancelAt) }),
      action: { href: SUBSCRIPTION_PAGE, label: t("choosePlanCta") },
    });
  } else if (isAdmin && subscription?.status === "Trialing" && subscription.trialEndsAt) {
    banners.push(
      subscription.isTrialExpired
        ? {
            key: "trial-expired",
            tone: "warn",
            chip: t("trialExpiredChip"),
            body: t("trialExpiredBody"),
            action: choosePlan,
          }
        : {
            key: "trial",
            tone: "info",
            chip: t("trialChip", { days: subscription.trialDaysRemaining ?? 0 }),
            body: t("trialBody", { date: formatDate(subscription.trialEndsAt) }),
            action: choosePlan,
          },
    );
  }

  if (banners.length === 0) return null;

  return (
    <div className="shrink-0">
      {banners.map((banner) => {
        const tone = TONE_STYLES[banner.tone];
        return (
          <div
            key={banner.key}
            role="status"
            className={`bg-white border-b border-rule border-l-4 ${tone.bar} px-6 py-2.5 flex flex-wrap items-center gap-x-3 gap-y-1.5`}
          >
            <Mono
              className={`text-[10.5px] font-semibold uppercase tracking-[0.06em] px-2 py-0.5 rounded-full shrink-0 ${tone.chip}`}
            >
              {banner.chip}
            </Mono>
            <p className="text-[12.5px] text-ink-2 m-0 flex-1 min-w-[200px]">{banner.body}</p>
            {banner.action && (
              <Link
                href={banner.action.href}
                className="shrink-0 px-3 py-1.5 text-[11px] text-white font-semibold bg-ink rounded hover:bg-ink/90 transition-colors"
              >
                {banner.action.label}
              </Link>
            )}
          </div>
        );
      })}
    </div>
  );
}
