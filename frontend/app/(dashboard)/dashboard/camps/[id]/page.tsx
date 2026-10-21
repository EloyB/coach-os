"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  ChevronLeft,
  Trash2,
  Users,
  Pencil,
  X,
  Euro,
  CalendarDays,
  Building2,
  GraduationCap,
  Clock,
  AlertTriangle,
  Download,
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
  exportCamp,
  type CampDetailDto,
} from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { formatDateNL } from "@/lib/date-utils";
import { getAuthUser } from "@/lib/auth";
import { downloadBlob } from "@/lib/download";
import { toast } from "sonner";
import { CampFormBuilder } from "../_components/camp-form-builder";
import { CampEditForm } from "../_components/camp-edit-form";
import { CampDaysCard } from "../_components/camp-days-card";
import { CampPricesCard } from "../_components/camp-prices-card";
import { CampDaysReadOnlyCard } from "../_components/camp-days-readonly-card";
import { CampEnrollmentsSection } from "../_components/camp-enrollments-section";

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
  const levelLabel =
    camp.level == null ? t("levelNone") : t(`level${camp.level}`);

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
  const tEnrollments = useTranslations("campEnrollmentsTable");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [deleteErrors, setDeleteErrors] = useState<string[]>([]);
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    setIsAdmin(getAuthUser()?.role === "Admin");
  }, []);

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

  const exportMutation = useMutation({
    mutationFn: () => exportCamp(id),
    onSuccess: (blob) => {
      downloadBlob(blob, `${camp?.name ?? "kamp"}-kamp.xlsx`);
      toast.success(tEnrollments("exportSuccess"));
    },
    onError: () => {
      toast.error(tEnrollments("exportError"));
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

              <div className="flex shrink-0 items-center gap-2">
                <button
                  type="button"
                  onClick={() => exportMutation.mutate()}
                  disabled={exportMutation.isPending}
                  aria-label={tEnrollments("exportAction")}
                  className="flex items-center gap-1.5 rounded-lg border border-gray-200 px-2 py-1.5 sm:px-3 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors disabled:opacity-50"
                >
                  <Download size={13} />
                  <span className="hidden sm:inline">
                    {exportMutation.isPending
                      ? tEnrollments("exporting")
                      : tEnrollments("exportAction")}
                  </span>
                </button>
                {isAdmin &&
                  (!editing ? (
                    <button
                      type="button"
                      onClick={() => setEditing(true)}
                      aria-label={t("editAction")}
                      className="shrink-0 flex items-center gap-1.5 rounded-lg border border-gray-200 px-2 py-1.5 sm:px-3 text-xs font-medium text-gray-600 hover:bg-gray-50 transition-colors"
                    >
                      <Pencil size={13} />
                      <span className="hidden sm:inline">
                        {t("editAction")}
                      </span>
                    </button>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setEditing(false)}
                      aria-label={t("close")}
                      className="shrink-0 flex items-center gap-1.5 rounded-lg border border-gray-200 px-2 py-1.5 sm:px-3 text-xs font-medium text-gray-500 hover:bg-gray-50 transition-colors"
                    >
                      <X size={13} />
                      <span className="hidden sm:inline">{t("close")}</span>
                    </button>
                  ))}
              </div>
            </div>

            {isAdmin && editing && (
              <CampEditForm
                camp={camp}
                onCancel={() => setEditing(false)}
                onSaved={() => setEditing(false)}
              />
            )}
          </div>

          {/* ── Section 2: Days & trainers ── */}
          {isAdmin ? (
            <CampDaysCard
              key={`${camp.id}:${camp.days
                .map(
                  (d) =>
                    `${d.date}|${d.startTime}|${d.endTime}|${d.trainers.length}`,
                )
                .join(",")}`}
              camp={camp}
            />
          ) : (
            <CampDaysReadOnlyCard camp={camp} />
          )}

          {/* ── Section 2b: Price options ── */}
          <CampPricesCard camp={camp} canEdit={isAdmin} />

          {/* ── Section 3: Form builder (admin only) ── */}
          {isAdmin && <CampFormBuilder campId={id} />}

          {/* ── Section 3: Enrollments ── */}
          <CampEnrollmentsSection campId={id} />

          {/* ── Section 4: Danger zone (admin only) ── */}
          {isAdmin && (
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
                        <AlertDialogCancel>
                          {t("deleteCancel")}
                        </AlertDialogCancel>
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
          )}
        </div>
      )}
    </>
  );
}
