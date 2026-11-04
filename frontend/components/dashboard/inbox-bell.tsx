"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Bell } from "lucide-react";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { getDashboardInbox, type InboxItemDto } from "@/lib/api/dashboard";
import { cn } from "@/lib/utils";

const INBOX_LIMIT = 25;

function inboxItemHref(item: InboxItemDto): string {
  if (item.type === "confirmation_pending") {
    return `/dashboard/lessons/${item.refId}/planning`;
  }
  if (item.type === "series_underbooked") {
    return `/dashboard/lessons/${item.refId}#enrollments`;
  }
  return `/dashboard/lessons/${item.refId}`;
}

function InboxCard({ item }: { item: InboxItemDto }) {
  const t = useTranslations("dashboard");
  const badge =
    item.type === "confirmation_pending"
      ? t("inboxBadgePending")
      : item.type === "series_underbooked"
        ? t("inboxBadgeUnderbooked")
        : t("inboxBadgeOther");
  const urgent = item.severity === "urgent";

  return (
    <li className="relative space-y-1.5 rounded-lg border border-rule bg-paper px-3.5 py-3 text-[13px] transition-colors hover:border-ink/40 hover:bg-canvas/60">
      <div className="flex items-center justify-between gap-3">
        <span
          className={cn(
            "rounded-full px-2 py-0.5 text-[10.5px] font-semibold",
            urgent ? "bg-red-100 text-red-700" : "bg-amber-100 text-amber-800",
          )}
        >
          {badge}
        </span>
        {item.meta && (
          <Mono className="shrink-0 text-[10.5px] text-ink-3">{item.meta}</Mono>
        )}
      </div>
      <p className="font-medium text-ink">
        {/* after:inset-0 rekt de link uit over de hele card. */}
        <Link
          href={inboxItemHref(item)}
          className="after:absolute after:inset-0 after:rounded-lg focus-visible:outline-none focus-visible:after:ring-2 focus-visible:after:ring-ink"
        >
          {item.title}
        </Link>
      </p>
      <p className="text-[12px] text-ink-3">{item.body}</p>
    </li>
  );
}

/** Bel in de topbar met het aantal dingen die actie vragen; een klik opent de lijst in een dialog. */
export function InboxBell() {
  const t = useTranslations("dashboard");
  const [open, setOpen] = useState(false);

  const { data: inbox } = useQuery({
    queryKey: ["dashboard", "inbox", INBOX_LIMIT],
    queryFn: () => getDashboardInbox(INBOX_LIMIT),
  });

  const total = inbox?.totalCount ?? 0;
  const items = inbox?.items ?? [];

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label={t("inboxBellAria", { count: total })}
        className="relative grid h-8 w-8 cursor-pointer place-items-center rounded-md text-ink-3 transition-colors hover:bg-canvas hover:text-ink focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ink"
      >
        <Bell size={17} />
        {total > 0 && (
          <span className="absolute -right-0.5 -top-0.5 grid h-[17px] min-w-[17px] place-items-center rounded-full bg-red-500 px-1 text-[10px] font-bold leading-none text-white">
            {total > 99 ? "99+" : total}
          </span>
        )}
      </button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("inboxDialogTitle")}</DialogTitle>
            <DialogDescription>
              {total > 0
                ? t("inboxDialogDescription", { count: total })
                : t("priorityEmptyDesc")}
            </DialogDescription>
          </DialogHeader>

          {total === 0 ? (
            <p className="rounded-lg border border-dashed border-rule px-3 py-6 text-center text-xs text-ink-3">
              {t("priorityEmpty")}
            </p>
          ) : (
            <>
              <ul className="space-y-2.5">
                {items.map((item, i) => (
                  <InboxCard
                    key={`${item.type}-${item.refId}-${i}`}
                    item={item}
                  />
                ))}
              </ul>
              {total > items.length && (
                <p className="text-center text-[11px] text-ink-3">
                  {t("inboxShowing", { shown: items.length, total })}
                </p>
              )}
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
