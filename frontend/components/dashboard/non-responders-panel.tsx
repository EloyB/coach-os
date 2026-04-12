"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Phone, MessageCircle, Copy, Check } from "lucide-react";
import { getNonResponders } from "@/lib/api/confirmation";
import type { NonResponderDto } from "@/lib/api/confirmation";

const DAY_NAMES_SHORT = ["Zo", "Ma", "Di", "Wo", "Do", "Vr", "Za"];

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

export function NonRespondersPanel({ seriesId }: { seriesId: string }) {
  const t = useTranslations("nonResponders");
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const { data: nonResponders = [] } = useQuery({
    queryKey: ["planning", seriesId, "non-responders"],
    queryFn: () => getNonResponders(seriesId),
    refetchInterval: 30000, // poll every 30s
  });

  if (nonResponders.length === 0) return null;

  function handleCopyEmail(nr: NonResponderDto) {
    navigator.clipboard.writeText(nr.studentEmail);
    setCopiedId(nr.assignmentId);
    setTimeout(() => setCopiedId(null), 2000);
  }

  return (
    <div className="p-4 border-b border-gray-100">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold text-gray-900">
          {t("title")} ({nonResponders.length})
        </h3>
      </div>

      <div className="space-y-2">
        {nonResponders.map((nr) => (
          <div
            key={nr.assignmentId}
            className={`border rounded-lg p-3 ${
              nr.isExpired
                ? "border-red-200 bg-red-50/50"
                : "border-amber-200 bg-amber-50/50"
            }`}
          >
            <div className="flex items-start justify-between gap-2 mb-1.5">
              <div className="min-w-0">
                <div className="text-xs font-medium text-gray-900 truncate">
                  {nr.studentName}
                  {nr.isGroup && (
                    <span className="text-gray-400 font-normal ml-1">
                      — {t("groupLeader", { size: nr.groupSize })}
                    </span>
                  )}
                </div>
                <div className="text-[10px] text-gray-500">
                  {DAY_NAMES_SHORT[nr.dayOfWeek]} {nr.startTime}
                  {nr.courtName && ` · ${nr.courtName}`}
                </div>
              </div>

              {/* Expiry badge */}
              {nr.isExpired ? (
                <span className="shrink-0 text-[10px] font-medium px-2 py-0.5 rounded-full bg-red-100 text-red-700">
                  {t("expired")}
                </span>
              ) : (
                <span className="shrink-0 text-[10px] font-medium px-2 py-0.5 rounded-full bg-amber-100 text-amber-700">
                  {formatRelativeExpiry(nr.expiresAt)}
                </span>
              )}
            </div>

            {/* Contact actions */}
            <div className="flex items-center gap-1.5 mt-2">
              {nr.studentPhone && (
                <>
                  <a
                    href={`tel:${nr.studentPhone}`}
                    className="inline-flex items-center gap-1 px-2 py-1 rounded text-[10px] text-gray-600 hover:bg-white hover:text-tennis-green border border-gray-200 transition-colors"
                  >
                    <Phone size={10} />
                    Bellen
                  </a>
                  <a
                    href={`https://wa.me/${nr.studentPhone.replace(/[^0-9+]/g, "").replace(/^\+/, "")}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1 px-2 py-1 rounded text-[10px] text-gray-600 hover:bg-white hover:text-green-600 border border-gray-200 transition-colors"
                  >
                    <MessageCircle size={10} />
                    WhatsApp
                  </a>
                </>
              )}
              <button
                type="button"
                onClick={() => handleCopyEmail(nr)}
                className="inline-flex items-center gap-1 px-2 py-1 rounded text-[10px] text-gray-600 hover:bg-white hover:text-tennis-green border border-gray-200 transition-colors"
              >
                {copiedId === nr.assignmentId ? (
                  <>
                    <Check size={10} className="text-green-500" />
                    {t("emailCopied")}
                  </>
                ) : (
                  <>
                    <Copy size={10} />
                    {t("copyEmail")}
                  </>
                )}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
