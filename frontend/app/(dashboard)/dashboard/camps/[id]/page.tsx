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
  updateCamp,
  deleteCamp,
  getCampEnrollments,
  type CampEnrollmentDto,
  type CreateCampRequest,
} from "@/lib/api/camps";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { getTrainers } from "@/lib/api/trainers";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { CampForm } from "../_components/camp-form";
import { CampFormBuilder } from "../_components/camp-form-builder";

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

function EnrollmentRow({ enrollment }: { enrollment: CampEnrollmentDto }) {
  const [expanded, setExpanded] = useState(false);
  const hasResponses = enrollment.formResponses.length > 0;

  return (
    <div className="border-b border-rule last:border-b-0">
      <div
        className={`flex items-center justify-between px-5 py-3 ${hasResponses ? "cursor-pointer hover:bg-canvas/50" : ""}`}
        onClick={() => hasResponses && setExpanded((v) => !v)}
      >
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-ink truncate">
            {enrollment.participantName}
          </p>
          <p className="text-xs text-ink-3 truncate">
            {enrollment.participantEmail}
            {enrollment.groupName ? ` · ${enrollment.groupName}` : ""}
          </p>
        </div>
        <div className="flex items-center gap-3 ml-4">
          <span className="text-xs text-ink-3">
            {new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
          </span>
          <StatusBadge status={enrollment.status} />
          {hasResponses &&
            (expanded ? (
              <ChevronUp size={13} className="text-ink-3" />
            ) : (
              <ChevronDown size={13} className="text-ink-3" />
            ))}
        </div>
      </div>

      {expanded && hasResponses && (
        <div className="px-5 pb-3">
          <dl className="space-y-1.5 bg-canvas rounded-lg p-3">
            {enrollment.formResponses.map((r, i) => (
              <div key={i} className="flex gap-3 text-xs">
                <dt className="text-ink-3 shrink-0 min-w-[120px]">
                  {r.fieldLabel}
                </dt>
                <dd className="text-ink font-medium">{r.value}</dd>
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
    <div className="bg-paper border border-rule rounded-xl overflow-hidden">
      <div className="px-5 py-4 border-b border-rule flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
            <Users size={13} className="text-tennis-green" />
          </div>
          <h2 className="text-sm font-semibold text-ink">
            {t("enrollmentsTitle")}
          </h2>
          <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-tennis-green/10 text-tennis-green text-xs font-bold">
            {enrollments.length}
          </span>
        </div>
        <button
          type="button"
          onClick={handleCopyLink}
          className="inline-flex items-center gap-1.5 text-xs font-medium text-ink-2 hover:text-ink"
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
        <div className="p-5 text-xs text-ink-3">{t("enrollmentsLoading")}</div>
      ) : enrollments.length === 0 ? (
        <p className="p-5 text-xs text-ink-3">{t("enrollmentsEmpty")}</p>
      ) : (
        <div>
          {enrollments.map((e) => (
            <EnrollmentRow key={e.id} enrollment={e} />
          ))}
        </div>
      )}
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
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [deleteErrors, setDeleteErrors] = useState<string[]>([]);

  const {
    data: camp,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["camp", id],
    queryFn: () => getCampById(id),
  });
  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });
  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const updateMutation = useMutation({
    mutationFn: (req: CreateCampRequest) =>
      updateCamp(id, { ...req, isActive: camp?.isActive ?? true }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      queryClient.invalidateQueries({ queryKey: ["camp", id] });
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
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

  if (isLoading) {
    return (
      <div className="max-w-3xl animate-pulse space-y-5">
        <div className="h-8 w-48 bg-canvas rounded" />
        <div className="h-64 bg-paper border border-rule rounded-xl" />
      </div>
    );
  }

  if (isError || !camp) {
    return (
      <div className="max-w-3xl">
        <Link
          href="/dashboard/camps"
          className="inline-flex items-center gap-1 text-xs text-ink-3 hover:text-ink-2 mb-4"
        >
          <ChevronLeft size={14} />
          {t("back")}
        </Link>
        <div className="bg-red-50 border border-red-100 rounded-xl p-5 text-sm text-red-600">
          {t("detailNotFound")}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-3xl space-y-5">
      <div className="flex items-start justify-between">
        <div>
          <Link
            href="/dashboard/camps"
            className="inline-flex items-center gap-1 text-xs text-ink-3 hover:text-ink-2 mb-3"
          >
            <ChevronLeft size={14} />
            {t("back")}
          </Link>
          <h1 className="text-lg font-bold text-ink tracking-tight">
            {t("editTitle")}
          </h1>
          <p className="text-sm text-ink-3 mt-0.5">{t("editSubtitle")}</p>
        </div>

        <div className="flex flex-col items-end gap-1.5">
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <button
                type="button"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-red-200 text-red-600 text-xs font-semibold rounded-md hover:bg-red-50 transition-colors"
              >
                <Trash2 size={13} />
                {t("delete")}
              </button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t("deleteConfirmTitle")}</AlertDialogTitle>
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
                  className="bg-red-600 hover:bg-red-700"
                >
                  {deleteMutation.isPending
                    ? t("deleting")
                    : t("deleteConfirm")}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
          {deleteErrors.length > 0 && (
            <div className="text-right text-[11px] text-red-600 space-y-0.5">
              {deleteErrors.map((msg, i) => (
                <p key={i}>{msg}</p>
              ))}
            </div>
          )}
        </div>
      </div>

      <CampForm
        initial={camp}
        clubs={clubs}
        trainers={trainers}
        onSubmit={(req) => {
          setErrorMessages([]);
          updateMutation.mutate(req);
        }}
        submitting={updateMutation.isPending}
        errorMessages={errorMessages}
      />

      <EnrollmentsSection campId={id} />

      <CampFormBuilder campId={id} />
    </div>
  );
}
