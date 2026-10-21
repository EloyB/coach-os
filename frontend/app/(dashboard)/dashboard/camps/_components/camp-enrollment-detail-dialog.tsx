"use client";

import { useTranslations } from "next-intl";
import { CheckCircle2, Mail, MessageCircle, Phone } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { btnPrimary } from "@/lib/styles";
import { enrollmentDisplayStatus } from "@/lib/status-styles";
import type { CampEnrollmentDto } from "@/lib/api/camps";

function DetailRow({
  label,
  value,
  href,
}: {
  label: string;
  value: string;
  href?: string;
}) {
  return (
    <div className="flex gap-3 text-sm">
      <dt className="w-32 shrink-0 text-gray-400">{label}</dt>
      <dd className="min-w-0 break-words font-medium text-gray-900">
        {href ? (
          <a href={href} className="text-tennis-green hover:underline">
            {value}
          </a>
        ) : (
          value
        )}
      </dd>
    </div>
  );
}

interface CampEnrollmentDetailDialogProps {
  enrollment: CampEnrollmentDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  canMarkPaid: boolean;
  isMarkingPaid: boolean;
  onMarkPaid: () => void;
  markPaidLabel: string;
}

export function CampEnrollmentDetailDialog({
  enrollment,
  open,
  onOpenChange,
  canMarkPaid,
  isMarkingPaid,
  onMarkPaid,
  markPaidLabel,
}: CampEnrollmentDetailDialogProps) {
  const t = useTranslations("campEnrollmentsTable");
  const style = enrollmentDisplayStatus(enrollment.status, enrollment.paymentStatus);
  const waPhone = enrollment.participantPhone?.replace(/[^\d]/g, "");

  const methodLabel =
    enrollment.paymentMethod === "Cash"
      ? t("paymentMethodCash")
      : enrollment.paymentMethod === "Online"
        ? t("paymentMethodOnline")
        : enrollment.paymentMethod === "Transfer"
          ? t("paymentMethodTransfer")
          : enrollment.paymentMethod;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onClick={(e) => e.stopPropagation()}
        className="flex max-h-[85vh] flex-col gap-0 overflow-hidden p-0 sm:max-w-xl"
      >
        <div className="flex flex-col gap-5 overflow-y-auto px-6 pb-6 pt-6">
          <DialogHeader>
            <DialogTitle className="flex flex-wrap items-center gap-2">
              {enrollment.participantName}
              {style && (
                <Badge className={`${style.className} border-0 text-xs`}>
                  {style.label}
                </Badge>
              )}
            </DialogTitle>
          </DialogHeader>

          <div className="flex flex-wrap gap-2">
            <a
              href={`mailto:${enrollment.participantEmail}`}
              className="inline-flex items-center gap-1.5 rounded-md border border-gray-200 px-2.5 py-1.5 text-xs font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              <Mail size={13} className="text-gray-400" />
              {t("contactEmail")}
            </a>
            {enrollment.participantPhone && (
              <>
                <a
                  href={`tel:${enrollment.participantPhone}`}
                  className="inline-flex items-center gap-1.5 rounded-md border border-gray-200 px-2.5 py-1.5 text-xs font-medium text-gray-700 transition-colors hover:bg-gray-50"
                >
                  <Phone size={13} className="text-gray-400" />
                  {t("contactCall")}
                </a>
                <a
                  href={`https://wa.me/${waPhone}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 rounded-md border border-gray-200 px-2.5 py-1.5 text-xs font-medium text-gray-700 transition-colors hover:bg-gray-50"
                >
                  <MessageCircle size={13} className="text-green-500" />
                  {t("contactWhatsapp")}
                </a>
              </>
            )}
          </div>

          <dl className="space-y-3.5">
            <DetailRow
              label={t("detailEmail")}
              value={enrollment.participantEmail}
              href={`mailto:${enrollment.participantEmail}`}
            />
            {enrollment.participantPhone && (
              <DetailRow
                label={t("detailPhone")}
                value={enrollment.participantPhone}
                href={`tel:${enrollment.participantPhone}`}
              />
            )}
            <DetailRow
              label={t("enrolledAt")}
              value={new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
            />
            {enrollment.campPriceLabel && (
              <DetailRow
                label={t("priceLabel")}
                value={enrollment.campPriceLabel}
              />
            )}
            {enrollment.groupName && (
              <DetailRow
                label={t("group")}
                value={
                  enrollment.isGroupLeader
                    ? `${enrollment.groupName} · ${t("leader")}`
                    : enrollment.groupName
                }
              />
            )}
            {methodLabel && (
              <DetailRow label={t("paymentMethod")} value={methodLabel} />
            )}
          </dl>

          <div>
            <h3 className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-gray-400">
              {t("sectionResponses")}
            </h3>
            {enrollment.formResponses.length === 0 ? (
              <p className="text-sm text-gray-400">{t("noResponses")}</p>
            ) : (
              <dl className="space-y-1.5 rounded-lg bg-[#FAFAF8] p-3">
                {enrollment.formResponses.map((r, i) => (
                  <div key={i} className="flex gap-3 text-sm">
                    <dt className="min-w-[120px] shrink-0 text-gray-400">
                      {r.fieldLabel}
                    </dt>
                    <dd className="font-medium text-gray-900">{r.value}</dd>
                  </div>
                ))}
              </dl>
            )}
          </div>

          {canMarkPaid && (
            <button
              type="button"
              onClick={onMarkPaid}
              disabled={isMarkingPaid}
              className={`${btnPrimary} inline-flex items-center justify-center gap-1.5 self-start disabled:opacity-60`}
            >
              <CheckCircle2 size={14} />
              {markPaidLabel}
            </button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
