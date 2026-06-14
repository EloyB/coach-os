"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  ChevronLeft,
  ChevronDown,
  ChevronUp,
  Trash2,
  Users,
  Copy,
  CheckCircle2,
  Pencil,
  X,
  Euro,
  CalendarDays,
  Building2,
  GraduationCap,
  Clock,
  AlertTriangle,
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
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  getCampById,
  deleteCamp,
  getCampEnrollments,
  markCampEnrollmentCashPaid,
  type CampEnrollmentDto,
  type CampDetailDto,
} from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { formatDateNL } from "@/lib/date-utils";
import { CampFormBuilder } from "../_components/camp-form-builder";
import { CampEditForm } from "../_components/camp-edit-form";
import { CampDaysCard } from "../_components/camp-days-card";

// ─── Enrollment status badges ─────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
  const t = useTranslations("camps");
  const map: Record<string, { label: string; className: string }> = {
    Pending: {
      label: t("statusPending"),
      className: "bg-amber-50 text-amber-700",
    },
    PendingPayment: {
      label: t("paymentPending"),
      className: "bg-amber-50 text-amber-700",
    },
    Confirmed: {
      label: t("statusConfirmed"),
      className: "bg-tennis-green/10 text-tennis-green",
    },
    Cancelled: {
      label: t("statusCancelled"),
      className: "bg-canvas text-ink-3",
    },
  };
  const entry = map[status] ?? { label: status, className: "bg-canvas text-ink-3" };
  return (
    <span
      className={`text-[10.5px] px-2 py-0.5 rounded-full font-semibold ${entry.className}`}
    >
      {entry.label}
    </span>
  );
}

function PaymentBadge({
  paymentMethod,
  paymentStatus,
}: {
  paymentMethod: string | null;
  paymentStatus: string | null;
}) {
  const t = useTranslations("camps");
  if (!paymentMethod && !paymentStatus) return null;

  let label: string;
  let className: string;
  if (paymentStatus === "Paid") {
    label = t("paymentPaid");
    className = "bg-tennis-green/10 text-tennis-green";
  } else if (paymentMethod === "Cash") {
    label = t("paymentCashPending");
    className = "bg-amber-50 text-amber-700";
  } else if (paymentMethod === "Online") {
    label = t("paymentOnlinePending");
    className = "bg-amber-50 text-amber-700";
  } else {
    label = t("paymentPending");
    className = "bg-amber-50 text-amber-700";
  }

  return (
    <span
      className={`text-[10.5px] px-2 py-0.5 rounded-full font-semibold ${className}`}
    >
      {label}
    </span>
  );
}

function EnrollmentRow({
  enrollment,
  campId,
}: {
  enrollment: CampEnrollmentDto;
  campId: string;
}) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [markErrors, setMarkErrors] = useState<string[]>([]);
  const hasResponses = enrollment.formResponses.length > 0;

  const isCashPending =
    enrollment.paymentMethod === "Cash" && enrollment.paymentStatus === "Pending";

  const markPaidMutation = useMutation({
    mutationFn: () => markCampEnrollmentCashPaid(campId, enrollment.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["campEnrollments", campId] });
      queryClient.invalidateQueries({ queryKey: ["camp", campId] });
    },
    onError: (error) => {
      setMarkErrors(getAxiosErrorMessages(error, t("markPaidError")));
    },
  });

  return (
    <div className="border-b border-gray-100 last:border-b-0">
      <div
        className={`flex items-center justify-between px-5 py-3 ${hasResponses ? "cursor-pointer hover:bg-gray-50" : ""}`}
        onClick={() => hasResponses && setExpanded((v) => !v)}
      >
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-900 truncate">
            {enrollment.participantName}
          </p>
          <p className="text-xs text-gray-400 truncate">
            {enrollment.participantEmail}
            {enrollment.groupName ? ` · ${enrollment.groupName}` : ""}
          </p>
        </div>
        <div className="flex items-center gap-3 ml-4">
          <span className="text-xs text-gray-400">
            {new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
          </span>
          <PaymentBadge
            paymentMethod={enrollment.paymentMethod}
            paymentStatus={enrollment.paymentStatus}
          />
          <StatusBadge status={enrollment.status} />
          {hasResponses &&
            (expanded ? (
              <ChevronUp size={13} className="text-gray-400" />
            ) : (
              <ChevronDown size={13} className="text-gray-400" />
            ))}
        </div>
      </div>

      {isCashPending && (
        <div
          className="px-5 pb-3"
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            onClick={() => {
              setMarkErrors([]);
              markPaidMutation.mutate();
            }}
            disabled={markPaidMutation.isPending}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-tennis-green text-white text-xs font-semibold hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            <CheckCircle2 size={13} />
            {markPaidMutation.isPending ? t("markingPaid") : t("markPaid")}
          </button>
          {markErrors.length > 0 && (
            <div className="text-xs text-red-600 mt-2 space-y-0.5">
              {markErrors.map((msg, i) => (
                <p key={i}>{msg}</p>
              ))}
            </div>
          )}
        </div>
      )}

      {expanded && hasResponses && (
        <div className="px-5 pb-3">
          <dl className="space-y-1.5 bg-[#FAFAF8] rounded-lg p-3">
            {enrollment.formResponses.map((r, i) => (
              <div key={i} className="flex gap-3 text-xs">
                <dt className="text-gray-400 shrink-0 min-w-[120px]">
                  {r.fieldLabel}
                </dt>
                <dd className="text-gray-900 font-medium">{r.value}</dd>
              </div>
            ))}
          </dl>
        </div>
      )}
    </div>
  );
}

function EnrollmentsSection({ campId }: { campId: string }) {
  const t = useTranslations("camps");
  const [copied, setCopied] = useState(false);

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ["campEnrollments", campId],
    queryFn: () => getCampEnrollments(campId),
  });

  function handleCopyLink() {
    const url = `${window.location.origin}/camp/${campId}`;
    navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
            <Users size={13} className="text-tennis-green" />
          </div>
          <h2 className="text-sm font-semibold text-gray-900">
            {t("enrollmentsTitle")}
          </h2>
          <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-tennis-green/10 text-tennis-green text-xs font-bold">
            {enrollments.length}
          </span>
        </div>
        <button
          type="button"
          onClick={handleCopyLink}
          className="inline-flex items-center gap-1.5 text-xs font-medium text-gray-600 hover:text-gray-900"
        >
          {copied ? (
            <>
              <CheckCircle2 size={13} className="text-tennis-green" />
              {t("linkCopied")}
            </>
          ) : (
            <>
              <Copy size={13} />
              {t("shareLink")}
            </>
          )}
        </button>
      </div>

      {isLoading ? (
        <div className="p-5 text-xs text-gray-400">{t("enrollmentsLoading")}</div>
      ) : enrollments.length === 0 ? (
        <p className="p-5 text-xs text-gray-400">{t("enrollmentsEmpty")}</p>
      ) : (
        <div>
          {enrollments.map((e) => (
            <EnrollmentRow key={e.id} enrollment={e} campId={campId} />
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Info chips ───────────────────────────────────────────────────────────────

function Chip({
  icon,
  children,
}: {
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
      {icon}
      {children}
    </span>
  );
}

function InfoCard({ camp }: { camp: CampDetailDto }) {
  const t = useTranslations("camps");
  const levelLabel = camp.level == null ? t("levelNone") : t(`level${camp.level}`);

  return (
    <div className="flex flex-wrap gap-2">
      <Chip icon={<Euro size={11} className="text-tennis-green" />}>
        {camp.price > 0 ? `€${camp.price}` : t("priceFree")}
      </Chip>
      <Chip icon={<CalendarDays size={11} className="text-tennis-green" />}>
        {formatDateNL(camp.startDate)} - {formatDateNL(camp.endDate)}
      </Chip>
      <Chip icon={<GraduationCap size={11} className="text-tennis-green" />}>
        {levelLabel}
      </Chip>
      {camp.tennisClubName && (
        <Chip icon={<Building2 size={11} className="text-tennis-green" />}>
          {camp.tennisClubName}
        </Chip>
      )}
      <Chip icon={<Users size={11} className="text-tennis-green" />}>
        {camp.maxParticipants == null
          ? t("occupancyValue", { count: camp.participantCount })
          : t("occupancyOfMax", {
              count: camp.participantCount,
              max: camp.maxParticipants,
            })}
      </Chip>
      <Chip icon={<Clock size={11} className="text-tennis-green" />}>
        {t("publicDeadlineLabel")}:{" "}
        {formatDateNL(camp.registrationDeadline.split("T")[0])}
      </Chip>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CampDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("camps");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [deleteErrors, setDeleteErrors] = useState<string[]>([]);

  const {
    data: camp,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["camp", id],
    queryFn: () => getCampById(id),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteCamp(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      router.push("/dashboard/camps");
    },
    onError: (error) => {
      setDeleteErrors(getAxiosErrorMessages(error, t("deleteError")));
    },
  });

  return (
    <>
      {/* Back */}
      <Link
        href="/dashboard/camps"
        className="inline-flex items-center gap-1.5 text-sm text-gray-400 hover:text-gray-600 transition-colors mb-6"
      >
        <ChevronLeft size={15} />
        {t("back")}
      </Link>

      {isLoading && (
        <div className="animate-pulse space-y-5">
          <div className="h-8 w-48 bg-gray-100 rounded" />
          <div className="h-64 bg-white rounded-xl shadow-sm shadow-gray-100" />
        </div>
      )}

      {isError && !isLoading && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-5 text-sm text-red-600">
          {t("detailNotFound")}{" "}
          <Link href="/dashboard/camps" className="underline font-medium">
            {t("back")}
          </Link>
        </div>
      )}

      {camp && (
        <div className="space-y-5">
          {/* ── Section 1: Camp info card ── */}
          <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2.5 flex-wrap mb-1.5">
                  <h1 className="text-xl font-bold text-gray-900 leading-tight">
                    {camp.name}
                  </h1>
                  <span
                    className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                      camp.isActive
                        ? "bg-green-50 text-green-700"
                        : "bg-gray-100 text-gray-500"
                    }`}
                  >
                    {camp.isActive ? t("statusActive") : t("statusDraft")}
                  </span>
                </div>
                {camp.description && (
                  <p className="text-sm text-gray-500 mb-3">
                    {camp.description}
                  </p>
                )}

                {!editing && <InfoCard camp={camp} />}
              </div>

              {!editing ? (
                <button
                  type="button"
                  onClick={() => setEditing(true)}
                  className="shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors"
                >
                  <Pencil size={12} />
                  {t("editAction")}
                </button>
              ) : (
                <button
                  type="button"
                  onClick={() => setEditing(false)}
                  className="shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs font-medium text-gray-500 hover:bg-gray-50 transition-colors"
                >
                  <X size={12} />
                  {t("close")}
                </button>
              )}
            </div>

            {editing && (
              <CampEditForm
                camp={camp}
                onCancel={() => setEditing(false)}
                onSaved={() => setEditing(false)}
              />
            )}
          </div>

          {/* ── Section 2: Days & trainers (editable) ── */}
          <CampDaysCard
            key={`${camp.id}:${camp.days
              .map((d) => `${d.date}|${d.startTime}|${d.endTime}|${d.trainers.length}`)
              .join(",")}`}
            camp={camp}
          />

          {/* ── Section 3: Form builder ── */}
          <CampFormBuilder campId={id} />

          {/* ── Section 3: Enrollments ── */}
          <EnrollmentsSection campId={id} />

          {/* ── Section 4: Danger zone ── */}
          <div className="border border-red-200 rounded-xl bg-red-50/30 p-5">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-red-100 flex items-center justify-center shrink-0 mt-0.5">
                <AlertTriangle size={15} className="text-red-500" />
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="text-sm font-semibold text-gray-800 mb-1">
                  {t("delete")}
                </h3>
                <p className="text-xs text-gray-500 mb-4 leading-relaxed">
                  {t("dangerZoneDesc")}
                </p>
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <button className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white text-xs font-semibold rounded-lg hover:bg-red-700 transition-colors">
                      <Trash2 size={13} />
                      {t("delete")}
                    </button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>
                        {t("deleteConfirmTitle")}
                      </AlertDialogTitle>
                      <AlertDialogDescription>
                        {t("deleteConfirmDesc")}
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t("deleteCancel")}</AlertDialogCancel>
                      <AlertDialogAction
                        onClick={() => {
                          setDeleteErrors([]);
                          deleteMutation.mutate();
                        }}
                        disabled={deleteMutation.isPending}
                        className="bg-red-600 hover:bg-red-700 text-white"
                      >
                        {deleteMutation.isPending
                          ? t("deleting")
                          : t("deleteConfirm")}
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>

                {deleteErrors.length > 0 && (
                  <div className="text-xs text-red-600 mt-3 space-y-0.5">
                    {deleteErrors.map((msg, i) => (
                      <p key={i}>{msg}</p>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
