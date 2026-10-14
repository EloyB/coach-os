"use client";

import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import {
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Copy,
  Download,
  Euro,
  Eye,
  MoreVertical,
  Pencil,
  Search,
  Trash2,
  UserPlus,
  Users,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
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
import {
  cancelCampEnrollment,
  cancelCampEnrollmentGroup,
  exportCamp,
  getCampEnrollments,
  markCampEnrollmentCashPaid,
  type CampEnrollmentDto,
} from "@/lib/api/camps";
import { enrollmentDisplayStatus } from "@/lib/status-styles";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { downloadBlob } from "@/lib/download";
import { canEditEnrollment, isEnrollmentManager } from "@/lib/auth";
import { CampAddMemberDialog } from "./camp-add-member-dialog";
import { CampEditEnrollmentDialog } from "./camp-edit-enrollment-dialog";
import { CampManualEnrollmentDialog } from "./camp-manual-enrollment-dialog";
import { CampEnrollmentDetailDialog } from "./camp-enrollment-detail-dialog";

// ─── Helpers ─────────────────────────────────────────────────────────────────

const COLS = 5;

function formatEnrolledAt(value: string): string {
  return new Date(value).toLocaleDateString("nl-BE", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function contactLine(e: CampEnrollmentDto): string {
  return e.participantPhone
    ? `${e.participantEmail} · ${e.participantPhone}`
    : e.participantEmail;
}

type Block =
  | { kind: "solo"; enrollment: CampEnrollmentDto }
  | {
      kind: "group";
      groupId: string;
      leader: CampEnrollmentDto;
      members: CampEnrollmentDto[];
    };

/**
 * Groepeert inschrijvingen in solo-rijen en groep-blokken. De leider staat bovenaan
 * de leden; blokken worden alfabetisch gesorteerd (groepen op leidernaam).
 */
function buildBlocks(enrollments: CampEnrollmentDto[]): Block[] {
  const byGroup = new Map<string, CampEnrollmentDto[]>();
  const solos: CampEnrollmentDto[] = [];

  for (const e of enrollments) {
    if (e.enrollmentGroupId == null) {
      solos.push(e);
    } else {
      const list = byGroup.get(e.enrollmentGroupId) ?? [];
      list.push(e);
      byGroup.set(e.enrollmentGroupId, list);
    }
  }

  const blocks: Block[] = [];
  for (const [groupId, members] of byGroup) {
    const leader = members.find((m) => m.isGroupLeader) ?? members[0];
    const sortedMembers = [...members].sort((a, b) => {
      if (a.id === leader.id) return -1;
      if (b.id === leader.id) return 1;
      return a.participantName.localeCompare(b.participantName);
    });
    blocks.push({
      kind: "group",
      groupId,
      leader,
      members: sortedMembers,
    });
  }
  for (const enrollment of solos) blocks.push({ kind: "solo", enrollment });

  const name = (b: Block) =>
    b.kind === "group"
      ? b.leader.participantName
      : b.enrollment.participantName;
  return blocks.sort((a, b) => name(a).localeCompare(name(b)));
}

function StatusBadge({
  status,
  paymentStatus,
  className = "",
}: {
  status: string;
  paymentStatus: string | null;
  className?: string;
}) {
  const style = enrollmentDisplayStatus(status, paymentStatus);
  if (!style) return null;
  return (
    <Badge className={`${style.className} border-0 ${className}`}>
      {style.label}
    </Badge>
  );
}

function isCashPending(e: CampEnrollmentDto): boolean {
  return e.paymentMethod === "Cash" && e.paymentStatus === "Pending";
}

// ─── Section headers ─────────────────────────────────────────────────────────

function SectionHeaderRow({ label, count }: { label: string; count: number }) {
  return (
    <tr>
      <td
        colSpan={COLS}
        className="border-t border-gray-100 bg-gray-50/70 px-4 py-2 text-[11px] font-semibold uppercase tracking-wide text-gray-500"
      >
        {label} <span className="text-gray-400">({count})</span>
      </td>
    </tr>
  );
}

function SectionHeading({ label, count }: { label: string; count: number }) {
  return (
    <div className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-gray-500">
      {label} <span className="text-gray-400">({count})</span>
    </div>
  );
}

/** Annuleren mag enkel admin/hoofdtrainer (zelfde regel als de backend). */
function useCanManage(): boolean {
  const [canManage, setCanManage] = useState(false);
  useEffect(() => {
    // Auth staat alleen in localStorage; pas na hydration kunnen acties zichtbaar worden.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setCanManage(isEnrollmentManager());
  }, []);
  return canManage;
}

/** Bewerken mag enkel een Admin (het endpoint is Admin-only). */
function useCanEdit(): boolean {
  const [canEdit, setCanEdit] = useState(false);
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setCanEdit(canEditEnrollment());
  }, []);
  return canEdit;
}

function useCancelMutation(
  campId: string,
  mutationFn: () => Promise<void>,
  successKey: "toastCancelled" | "toastGroupCancelled",
) {
  const t = useTranslations("campEnrollmentsTable");
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["campEnrollments", campId] });
      queryClient.invalidateQueries({ queryKey: ["camp", campId] });
      toast.success(t(successKey));
    },
    onError: (error) => {
      toast.error(getAxiosErrorMessages(error, t("cancelError")).join(" "));
    },
  });
}

function CancelConfirmDialog({
  open,
  onOpenChange,
  title,
  body,
  showPaidWarning,
  isPending,
  onConfirm,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  body: string;
  showPaidWarning: boolean;
  isPending: boolean;
  onConfirm: () => void;
}) {
  const t = useTranslations("campEnrollmentsTable");
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent onClick={(e) => e.stopPropagation()}>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>
            {body}
            {showPaidWarning && (
              <span className="mt-2 block font-medium text-amber-700">
                {t("cancelPaidWarning")}
              </span>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t("back")}</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={onConfirm}
            className="bg-red-600 text-white hover:bg-red-700"
          >
            {t("confirmCancel")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

function LeaderLabel() {
  const t = useTranslations("campEnrollmentsTable");
  return (
    <span className="ml-1.5 text-[11px] font-normal text-gray-400">
      {t("leader")}
    </span>
  );
}

// ─── Rij-acties (⋮-menu + detail-dialog) ─────────────────────────────────────

function useEnrollmentActions(
  campId: string,
  enrollment: CampEnrollmentDto,
  isGroup = false,
) {
  const t = useTranslations("campEnrollmentsTable");
  const queryClient = useQueryClient();
  const [detailOpen, setDetailOpen] = useState(false);

  const markPaidMutation = useMutation({
    mutationFn: () => markCampEnrollmentCashPaid(campId, enrollment.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["campEnrollments", campId] });
      queryClient.invalidateQueries({ queryKey: ["camp", campId] });
      toast.success(t(isGroup ? "toastGroupMarkedPaid" : "toastMarkedPaid"));
    },
    onError: (error) => {
      toast.error(getAxiosErrorMessages(error, t("markPaidError")).join(" "));
    },
  });

  return { detailOpen, setDetailOpen, markPaidMutation };
}

function ActionsMenu({
  enrollment,
  menuKey,
  openMenuId,
  setOpenMenuId,
  onViewDetails,
  onMarkPaid,
  isMarkingPaid,
  markPaidLabel,
  canCancel,
  onCancel,
  canEdit,
  onEdit,
}: {
  enrollment: CampEnrollmentDto;
  menuKey: string;
  openMenuId: string | null;
  setOpenMenuId: (id: string | null) => void;
  onViewDetails: () => void;
  onMarkPaid: () => void;
  isMarkingPaid: boolean;
  markPaidLabel: string;
  canCancel: boolean;
  onCancel: () => void;
  canEdit: boolean;
  onEdit: () => void;
}) {
  const t = useTranslations("campEnrollmentsTable");
  return (
    <Popover
      open={openMenuId === menuKey}
      onOpenChange={(o) => setOpenMenuId(o ? menuKey : null)}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          onClick={(e) => e.stopPropagation()}
          aria-label={t("actionsLabel", { name: enrollment.participantName })}
          className="flex h-8 w-8 items-center justify-center rounded-md border border-gray-100 text-gray-400 hover:bg-gray-50 hover:text-gray-700"
        >
          <MoreVertical size={15} />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        onClick={(e) => e.stopPropagation()}
        className="w-52 p-1 text-sm"
      >
        <button
          type="button"
          onClick={() => {
            setOpenMenuId(null);
            onViewDetails();
          }}
          className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
        >
          <Eye size={13} />
          {t("viewDetails")}
        </button>
        {isCashPending(enrollment) && (
          <button
            type="button"
            disabled={isMarkingPaid}
            onClick={() => {
              setOpenMenuId(null);
              onMarkPaid();
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
          >
            <Euro size={13} />
            {markPaidLabel}
          </button>
        )}
        {canEdit && (
          <button
            type="button"
            onClick={() => {
              setOpenMenuId(null);
              onEdit();
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-gray-700 hover:bg-tennis-green/5 hover:text-tennis-green"
          >
            <Pencil size={13} />
            {t("editAction")}
          </button>
        )}
        {canCancel && (
          <button
            type="button"
            onClick={() => {
              setOpenMenuId(null);
              onCancel();
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-red-600 hover:bg-red-50"
          >
            <Trash2 size={13} />
            {t("cancelAction")}
          </button>
        )}
      </PopoverContent>
    </Popover>
  );
}

/** ⋮-menu op de groepskop: groep als betaald markeren en/of de groep annuleren. */
function GroupActionsMenu({
  leader,
  menuKey,
  openMenuId,
  setOpenMenuId,
  onMarkGroupPaid,
  isMarkingPaid,
  canCancel,
  onCancelGroup,
  canAddMember,
  onAddMember,
}: {
  leader: CampEnrollmentDto;
  menuKey: string;
  openMenuId: string | null;
  setOpenMenuId: (id: string | null) => void;
  onMarkGroupPaid: () => void;
  isMarkingPaid: boolean;
  canCancel: boolean;
  onCancelGroup: () => void;
  canAddMember: boolean;
  onAddMember: () => void;
}) {
  const t = useTranslations("campEnrollmentsTable");
  const canMarkPaid = isCashPending(leader);
  if (!canMarkPaid && !canCancel && !canAddMember) return null;
  return (
    <Popover
      open={openMenuId === menuKey}
      onOpenChange={(o) => setOpenMenuId(o ? menuKey : null)}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          onClick={(e) => e.stopPropagation()}
          aria-label={t("actionsLabelGroup", { name: leader.participantName })}
          className="flex h-8 w-8 items-center justify-center rounded-md border border-gray-100 text-gray-400 hover:bg-gray-50 hover:text-gray-700"
        >
          <MoreVertical size={15} />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        onClick={(e) => e.stopPropagation()}
        className="w-60 p-1 text-sm"
      >
        {canMarkPaid && (
          <button
            type="button"
            disabled={isMarkingPaid}
            onClick={() => {
              setOpenMenuId(null);
              onMarkGroupPaid();
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
          >
            <Euro size={13} />
            {t("markGroupPaid")}
          </button>
        )}
        {canAddMember && (
          <button
            type="button"
            onClick={() => {
              setOpenMenuId(null);
              onAddMember();
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
          >
            <UserPlus size={13} />
            {t("addMember")}
          </button>
        )}
        {canCancel && (
          <button
            type="button"
            onClick={() => {
              setOpenMenuId(null);
              onCancelGroup();
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-red-600 hover:bg-red-50"
          >
            <Trash2 size={13} />
            {t("cancelGroup")}
          </button>
        )}
      </PopoverContent>
    </Popover>
  );
}

// ─── Persoonsrij (solo of groepslid) ─────────────────────────────────────────

function PersonRow({
  enrollment,
  campId,
  isMember,
  isLeader = false,
  isGroupPayer = false,
  cancelBlocked = false,
  groupPayment = null,
  isMatch,
  openMenuId,
  setOpenMenuId,
  variant = "row",
}: {
  enrollment: CampEnrollmentDto;
  campId: string;
  isMember: boolean;
  isLeader?: boolean;
  /** Leider van een groep: markeren als betaald geldt voor de hele groep. */
  isGroupPayer?: boolean;
  /** Leider met nog actieve leden: enkel de hele groep kan geannuleerd worden. */
  cancelBlocked?: boolean;
  /** Betaalinfo van de leider; leden hebben zelf geen betaling (groepsbetaling). */
  groupPayment?: { method: string | null; status: string | null } | null;
  isMatch: boolean;
  openMenuId: string | null;
  setOpenMenuId: (id: string | null) => void;
  variant?: "row" | "card";
}) {
  const t = useTranslations("campEnrollmentsTable");
  const { detailOpen, setDetailOpen, markPaidMutation } = useEnrollmentActions(
    campId,
    enrollment,
    isGroupPayer,
  );
  const isCancelled = enrollment.status === "Cancelled";
  const canManage = useCanManage();
  const canEdit = useCanEdit();
  const [editOpen, setEditOpen] = useState(false);
  const [confirmCancelOpen, setConfirmCancelOpen] = useState(false);
  const cancelMutation = useCancelMutation(
    campId,
    () => cancelCampEnrollment(campId, enrollment.id),
    "toastCancelled",
  );
  // Voor weergave: leden erven de betaalinfo van de groepsbetaling van de leider.
  // Acties blijven op de eigen (originele) betaling gebaseerd.
  const shown: CampEnrollmentDto = groupPayment
    ? {
        ...enrollment,
        paymentMethod: enrollment.paymentMethod ?? groupPayment.method,
        paymentStatus: enrollment.paymentStatus ?? groupPayment.status,
      }
    : enrollment;
  const markPaidLabel = isGroupPayer ? t("markGroupPaid") : t("markPaid");

  const menu = (
    <ActionsMenu
      enrollment={enrollment}
      menuKey={variant === "card" ? `card:${enrollment.id}` : enrollment.id}
      openMenuId={openMenuId}
      setOpenMenuId={setOpenMenuId}
      onViewDetails={() => setDetailOpen(true)}
      onMarkPaid={() => markPaidMutation.mutate()}
      isMarkingPaid={markPaidMutation.isPending}
      markPaidLabel={markPaidLabel}
      canCancel={canManage && !isCancelled && !cancelBlocked}
      onCancel={() => setConfirmCancelOpen(true)}
      canEdit={canEdit && !isCancelled}
      onEdit={() => setEditOpen(true)}
    />
  );

  const dialog = (
    <>
      <CampEditEnrollmentDialog
        campId={campId}
        enrollment={enrollment}
        open={editOpen}
        onOpenChange={setEditOpen}
      />
      <CancelConfirmDialog
        open={confirmCancelOpen}
        onOpenChange={setConfirmCancelOpen}
        title={t("cancelTitle")}
        body={t("cancelBody", { name: enrollment.participantName })}
        showPaidWarning={enrollment.paymentStatus === "Paid"}
        isPending={cancelMutation.isPending}
        onConfirm={() => cancelMutation.mutate()}
      />
      <CampEnrollmentDetailDialog
        enrollment={shown}
        markPaidLabel={markPaidLabel}
        open={detailOpen}
        onOpenChange={setDetailOpen}
        canMarkPaid={isCashPending(enrollment)}
        isMarkingPaid={markPaidMutation.isPending}
        onMarkPaid={() => markPaidMutation.mutate()}
      />
    </>
  );

  if (variant === "card") {
    return (
      <>
        <div
          onClick={() => setDetailOpen(true)}
          className={`cursor-pointer rounded-xl border bg-white p-3 hover:border-gray-200 ${
            isMatch
              ? "border-tennis-green/40 bg-tennis-green/5"
              : "border-gray-100"
          } ${isCancelled ? "opacity-60" : ""}`}
        >
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-gray-900">
                {enrollment.participantName}
                {isLeader && <LeaderLabel />}
              </p>
              <p className="mt-0.5 break-all text-xs text-gray-500">
                {contactLine(enrollment)}
              </p>
              <p className="mt-0.5 text-xs text-gray-400">
                {formatEnrolledAt(enrollment.enrolledAt)}
              </p>
              <StatusBadge
                status={shown.status}
                paymentStatus={shown.paymentStatus}
                className="mt-1.5 text-xs"
              />
            </div>
            <div onClick={(e) => e.stopPropagation()} className="shrink-0">
              {menu}
            </div>
          </div>
        </div>
        {dialog}
      </>
    );
  }

  return (
    <>
      <tr
        onClick={() => setDetailOpen(true)}
        className={`cursor-pointer border-t border-gray-100 hover:bg-gray-50/60 ${
          isMatch ? "bg-tennis-green/5" : ""
        } ${isCancelled ? "opacity-60" : ""}`}
      >
        <td className={`px-4 py-2.5 ${isMember ? "pl-12" : ""}`}>
          <span className="block truncate text-sm font-medium text-gray-900">
            {enrollment.participantName}
            {isLeader && <LeaderLabel />}
          </span>
        </td>
        <td className="px-4 py-2.5 text-xs text-gray-500">
          <span className="block truncate">{contactLine(enrollment)}</span>
        </td>
        <td className="whitespace-nowrap px-4 py-2.5 text-xs text-gray-500">
          {formatEnrolledAt(enrollment.enrolledAt)}
        </td>
        <td className="px-4 py-2.5">
          <StatusBadge
            status={shown.status}
            paymentStatus={shown.paymentStatus}
            className="text-xs"
          />
        </td>
        <td
          className="whitespace-nowrap px-4 py-2.5 text-right"
          onClick={(e) => e.stopPropagation()}
        >
          {menu}
        </td>
      </tr>
      {dialog}
    </>
  );
}

// ─── Groepsblok ──────────────────────────────────────────────────────────────

function GroupBlockRows({
  block,
  campId,
  forceExpanded,
  matchedIds,
  openMenuId,
  setOpenMenuId,
  variant = "row",
}: {
  block: Extract<Block, { kind: "group" }>;
  campId: string;
  forceExpanded: boolean;
  matchedIds: Set<string> | null;
  openMenuId: string | null;
  setOpenMenuId: (id: string | null) => void;
  variant?: "row" | "card";
}) {
  const t = useTranslations("campEnrollmentsTable");
  const [open, setOpen] = useState(false);
  const expanded = forceExpanded || open;
  const { leader, members } = block;
  const { markPaidMutation } = useEnrollmentActions(campId, leader, true);
  const canManage = useCanManage();
  const [confirmCancelOpen, setConfirmCancelOpen] = useState(false);
  const cancelGroupMutation = useCancelMutation(
    campId,
    () => cancelCampEnrollmentGroup(campId, block.groupId),
    "toastGroupCancelled",
  );
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const activeMembers = members.filter((m) => m.status !== "Cancelled");
  // De groepsbetaling staat vast zodra ze betaald is of online loopt (backend is leidend).
  const paymentLocked =
    leader.paymentStatus === "Paid" ||
    (leader.paymentStatus === "Pending" && leader.paymentMethod === "Online");
  const canAddMember =
    canManage && leader.status !== "Cancelled" && !paymentLocked;
  // Mobiele kaart en desktoprij staan beide in de DOM (één verborgen via CSS); een
  // variant-specifieke key voorkomt dat beide popovers tegelijk opengaan.
  const groupMenuKey =
    variant === "card"
      ? `card:group:${block.groupId}`
      : `group:${block.groupId}`;
  const groupPayment = {
    method: leader.paymentMethod,
    status: leader.paymentStatus,
  };
  const groupMenu = (
    <GroupActionsMenu
      leader={leader}
      menuKey={groupMenuKey}
      openMenuId={openMenuId}
      setOpenMenuId={setOpenMenuId}
      onMarkGroupPaid={() => markPaidMutation.mutate()}
      isMarkingPaid={markPaidMutation.isPending}
      canCancel={canManage && activeMembers.length > 0}
      onCancelGroup={() => setConfirmCancelOpen(true)}
      canAddMember={canAddMember}
      onAddMember={() => setAddMemberOpen(true)}
    />
  );
  const addMemberDialog = (
    <CampAddMemberDialog
      campId={campId}
      groupId={block.groupId}
      open={addMemberOpen}
      onOpenChange={setAddMemberOpen}
    />
  );
  const cancelGroupDialog = (
    <CancelConfirmDialog
      open={confirmCancelOpen}
      onOpenChange={setConfirmCancelOpen}
      title={t("cancelGroupTitle")}
      body={t("cancelGroupBody", { count: activeMembers.length })}
      showPaidWarning={leader.paymentStatus === "Paid"}
      isPending={cancelGroupMutation.isPending}
      onConfirm={() => cancelGroupMutation.mutate()}
    />
  );

  const membersBadge = (
    <Badge className="shrink-0 border-0 bg-tennis-green/10 text-[11px] text-tennis-green">
      {t("members", { count: members.length })}
    </Badge>
  );

  if (variant === "card") {
    return (
      <div className="space-y-2">
        {cancelGroupDialog}
        {addMemberDialog}
        <div
          onClick={() => setOpen((v) => !v)}
          className="cursor-pointer rounded-xl border border-gray-100 bg-gray-50/60 p-3 hover:border-gray-200"
        >
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                <Users size={14} className="shrink-0 text-tennis-green" />
                <span className="text-sm font-semibold text-gray-800">
                  {t("group")} · {leader.participantName}
                </span>
                {membersBadge}
              </div>
              <p className="mt-1 text-xs text-gray-500">
                {formatEnrolledAt(leader.enrolledAt)}
              </p>
              <StatusBadge
                status={leader.status}
                paymentStatus={leader.paymentStatus}
                className="mt-1.5 text-xs"
              />
            </div>
            <div onClick={(e) => e.stopPropagation()} className="shrink-0">
              {groupMenu}
            </div>
          </div>
        </div>
        {expanded &&
          members.map((m) => (
            <div key={m.id} className="ml-4">
              <PersonRow
                enrollment={m}
                campId={campId}
                isMember
                isLeader={m.id === leader.id}
                isGroupPayer={m.id === leader.id && members.length > 1}
                cancelBlocked={
                  m.id === leader.id &&
                  members.some(
                    (o) => o.id !== leader.id && o.status !== "Cancelled",
                  )
                }
                groupPayment={m.id === leader.id ? null : groupPayment}
                isMatch={matchedIds?.has(m.id) ?? false}
                openMenuId={openMenuId}
                setOpenMenuId={setOpenMenuId}
                variant="card"
              />
            </div>
          ))}
      </div>
    );
  }

  return (
    <>
      {cancelGroupDialog}
      {addMemberDialog}
      <tr
        className="cursor-pointer border-t border-gray-100 bg-gray-50/40 hover:bg-gray-50"
        onClick={() => setOpen((v) => !v)}
      >
        <td className="px-4 py-2.5" colSpan={2}>
          <div className="flex min-w-0 items-center gap-2">
            <span
              aria-label={expanded ? t("collapseGroup") : t("expandGroup")}
              className="-m-1 shrink-0 rounded p-1 text-gray-400"
            >
              {expanded ? (
                <ChevronDown size={15} />
              ) : (
                <ChevronRight size={15} />
              )}
            </span>
            <Users size={14} className="shrink-0 text-tennis-green" />
            <span className="truncate text-sm font-semibold text-gray-800">
              {t("group")} · {leader.participantName}
            </span>
            {membersBadge}
          </div>
        </td>
        <td className="whitespace-nowrap px-4 py-2.5 text-xs text-gray-500">
          {formatEnrolledAt(leader.enrolledAt)}
        </td>
        <td className="px-4 py-2.5">
          <StatusBadge
            status={leader.status}
            paymentStatus={leader.paymentStatus}
            className="text-xs"
          />
        </td>
        <td
          className="whitespace-nowrap px-4 py-2.5 text-right"
          onClick={(e) => e.stopPropagation()}
        >
          {groupMenu}
        </td>
      </tr>
      {expanded &&
        members.map((m) => (
          <PersonRow
            key={m.id}
            enrollment={m}
            campId={campId}
            isMember
            isLeader={m.id === leader.id}
            isGroupPayer={m.id === leader.id && members.length > 1}
            cancelBlocked={
              m.id === leader.id &&
              members.some(
                (o) => o.id !== leader.id && o.status !== "Cancelled",
              )
            }
            groupPayment={m.id === leader.id ? null : groupPayment}
            isMatch={matchedIds?.has(m.id) ?? false}
            openMenuId={openMenuId}
            setOpenMenuId={setOpenMenuId}
          />
        ))}
    </>
  );
}

// ─── Tabel (zoeken + secties) ────────────────────────────────────────────────

function EnrollmentsTable({
  enrollments,
  campId,
}: {
  enrollments: CampEnrollmentDto[];
  campId: string;
}) {
  const t = useTranslations("campEnrollmentsTable");
  const [query, setQuery] = useState("");
  const [showCancelled, setShowCancelled] = useState(false);
  // Eén open acties-menu tegelijk, gedeeld over alle rijen.
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);

  const cancelledCount = useMemo(
    () => enrollments.filter((e) => e.status === "Cancelled").length,
    [enrollments],
  );

  const visible = useMemo(
    () =>
      showCancelled
        ? enrollments
        : enrollments.filter((e) => e.status !== "Cancelled"),
    [enrollments, showCancelled],
  );

  const blocks = useMemo(() => buildBlocks(visible), [visible]);

  const q = query.trim().toLowerCase();

  // Bij zoeken: enkel matchende blokken; groepen uitgeklapt; matchende leden gehighlight.
  const { filteredBlocks, matchedIds } = useMemo(() => {
    if (!q)
      return { filteredBlocks: blocks, matchedIds: null as Set<string> | null };
    const matches = (e: CampEnrollmentDto) =>
      e.participantName.toLowerCase().includes(q) ||
      e.participantEmail.toLowerCase().includes(q);
    const ids = new Set<string>();
    const kept = blocks.filter((b) => {
      if (b.kind === "solo") {
        if (matches(b.enrollment)) {
          ids.add(b.enrollment.id);
          return true;
        }
        return false;
      }
      const memberMatches = b.members.filter(matches);
      memberMatches.forEach((m) => ids.add(m.id));
      return memberMatches.length > 0;
    });
    return { filteredBlocks: kept, matchedIds: ids };
  }, [blocks, q]);

  const groupBlocks = filteredBlocks.filter(
    (b): b is Extract<Block, { kind: "group" }> => b.kind === "group",
  );
  const soloBlocks = filteredBlocks.filter(
    (b): b is Extract<Block, { kind: "solo" }> => b.kind === "solo",
  );

  return (
    <div>
      <div className="flex items-center gap-3 border-b border-gray-100 px-5 py-3">
        <div className="relative max-w-xs flex-1">
          <Search
            size={15}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t("searchPlaceholder")}
            className="w-full rounded-lg border border-gray-200 py-2 pl-9 pr-3 text-sm outline-none focus:border-tennis-green focus:ring-2 focus:ring-tennis-green/20"
          />
        </div>
        {cancelledCount > 0 && (
          <button
            type="button"
            onClick={() => setShowCancelled((v) => !v)}
            className="text-xs font-medium text-gray-500 hover:text-gray-700"
          >
            {showCancelled
              ? t("hideCancelled")
              : t("showCancelled", { count: cancelledCount })}
          </button>
        )}
      </div>

      {filteredBlocks.length === 0 ? (
        <div className="py-10 text-center text-sm text-gray-400">
          {q ? t("noResults", { query }) : t("empty")}
        </div>
      ) : (
        <>
          {/* Mobiel: kaartenlijst */}
          <div className="space-y-5 p-4 sm:hidden">
            {soloBlocks.length > 0 && (
              <div>
                <SectionHeading
                  label={t("sectionIndividual")}
                  count={soloBlocks.length}
                />
                <div className="space-y-2">
                  {soloBlocks.map((block) => (
                    <PersonRow
                      key={block.enrollment.id}
                      enrollment={block.enrollment}
                      campId={campId}
                      isMember={false}
                      isMatch={matchedIds?.has(block.enrollment.id) ?? false}
                      openMenuId={openMenuId}
                      setOpenMenuId={setOpenMenuId}
                      variant="card"
                    />
                  ))}
                </div>
              </div>
            )}
            {groupBlocks.length > 0 && (
              <div>
                <SectionHeading
                  label={t("sectionGroups")}
                  count={groupBlocks.length}
                />
                <div className="space-y-2">
                  {groupBlocks.map((block) => (
                    <GroupBlockRows
                      key={block.groupId}
                      block={block}
                      campId={campId}
                      forceExpanded={!!q}
                      matchedIds={matchedIds}
                      openMenuId={openMenuId}
                      setOpenMenuId={setOpenMenuId}
                      variant="card"
                    />
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Desktop: tabel */}
          <div className="hidden overflow-x-auto sm:block">
            <table className="w-full min-w-[640px] table-fixed border-collapse text-left [&_td]:border-r [&_td]:border-gray-100 [&_th]:border-r [&_th]:border-gray-100 [&_td:last-child]:border-r-0 [&_th:last-child]:border-r-0">
              <colgroup>
                <col style={{ width: "34%" }} />
                <col style={{ width: "30%" }} />
                <col style={{ width: "14%" }} />
                <col style={{ width: "16%" }} />
                <col style={{ width: "6%" }} />
              </colgroup>
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50/70 text-[11px] font-semibold uppercase tracking-wide text-gray-400">
                  <th className="px-4 py-2 font-semibold">{t("colName")}</th>
                  <th className="px-4 py-2 font-semibold">{t("colContact")}</th>
                  <th className="px-4 py-2 font-semibold">
                    {t("colEnrolled")}
                  </th>
                  <th className="px-4 py-2 font-semibold">{t("colStatus")}</th>
                  <th className="px-4 py-2" />
                </tr>
              </thead>
              <tbody>
                {soloBlocks.length > 0 && (
                  <SectionHeaderRow
                    label={t("sectionIndividual")}
                    count={soloBlocks.length}
                  />
                )}
                {soloBlocks.map((block) => (
                  <PersonRow
                    key={block.enrollment.id}
                    enrollment={block.enrollment}
                    campId={campId}
                    isMember={false}
                    isMatch={matchedIds?.has(block.enrollment.id) ?? false}
                    openMenuId={openMenuId}
                    setOpenMenuId={setOpenMenuId}
                  />
                ))}
                {groupBlocks.length > 0 && (
                  <SectionHeaderRow
                    label={t("sectionGroups")}
                    count={groupBlocks.length}
                  />
                )}
                {groupBlocks.map((block) => (
                  <GroupBlockRows
                    key={block.groupId}
                    block={block}
                    campId={campId}
                    forceExpanded={!!q}
                    matchedIds={matchedIds}
                    openMenuId={openMenuId}
                    setOpenMenuId={setOpenMenuId}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}

// ─── Sectie (fetch + kop + tabel) ────────────────────────────────────────────

export function CampEnrollmentsSection({
  campId,
  campName,
}: {
  campId: string;
  campName: string;
}) {
  const t = useTranslations("campEnrollmentsTable");
  const [copied, setCopied] = useState(false);
  const [headerMenuOpen, setHeaderMenuOpen] = useState(false);
  const [manualOpen, setManualOpen] = useState(false);
  const canManage = useCanManage();

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ["campEnrollments", campId],
    queryFn: () => getCampEnrollments(campId),
  });

  const exportMutation = useMutation({
    mutationFn: () => exportCamp(campId),
    onSuccess: (blob) => {
      downloadBlob(blob, `${campName}-kamp.xlsx`);
      toast.success(t("exportSuccess"));
    },
    onError: () => {
      toast.error(t("exportError"));
    },
  });

  const activeCount = enrollments.filter(
    (e) =>
      e.status === "Confirmed" ||
      e.status === "Pending" ||
      e.status === "PendingPayment",
  ).length;

  function handleCopyLink() {
    const url = `${window.location.origin}/camp/${campId}`;
    navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div
      id="enrollments"
      className="scroll-mt-20 overflow-visible rounded-xl bg-white shadow-sm shadow-gray-100"
    >
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-100 px-5 py-4">
        <div className="flex items-center gap-2.5">
          <h2 className="text-sm font-semibold text-gray-800">{t("title")}</h2>
          <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-tennis-green/10 text-xs font-bold text-tennis-green">
            {activeCount}
          </span>
        </div>
        <div className="flex items-center gap-2">
          {/* Desktop: knoppen inline */}
          {canManage && (
            <button
              type="button"
              onClick={() => setManualOpen(true)}
              className="hidden rounded-lg bg-tennis-green px-3 py-1.5 text-xs font-semibold text-white hover:bg-tennis-green/90 sm:block"
            >
              {t("manualAdd")}
            </button>
          )}
          <button
            type="button"
            onClick={() => exportMutation.mutate()}
            disabled={exportMutation.isPending}
            className="hidden items-center gap-1.5 rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-50 disabled:opacity-50 sm:flex"
          >
            <Download size={12} />
            {exportMutation.isPending ? t("exporting") : t("exportAction")}
          </button>
          <button
            type="button"
            onClick={handleCopyLink}
            className="hidden items-center gap-1.5 rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-50 sm:flex"
          >
            {copied ? (
              <>
                <CheckCircle2 size={12} className="text-green-500" />
                {t("copied")}
              </>
            ) : (
              <>
                <Copy size={12} />
                {t("copyLink")}
              </>
            )}
          </button>

          {/* Mobiel: ⋮-menu met dezelfde acties */}
          <Popover open={headerMenuOpen} onOpenChange={setHeaderMenuOpen}>
            <PopoverTrigger asChild>
              <button
                type="button"
                aria-label={t("moreActions")}
                className="flex h-8 w-8 items-center justify-center rounded-lg border border-gray-200 text-gray-500 hover:bg-gray-50 sm:hidden"
              >
                <MoreVertical size={16} />
              </button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-52 p-1 text-sm">
              {canManage && (
                <button
                  type="button"
                  onClick={() => {
                    setHeaderMenuOpen(false);
                    setManualOpen(true);
                  }}
                  className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
                >
                  <UserPlus size={14} />
                  {t("manualAdd")}
                </button>
              )}
              <button
                type="button"
                disabled={exportMutation.isPending}
                onClick={() => {
                  setHeaderMenuOpen(false);
                  exportMutation.mutate();
                }}
                className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                <Download size={14} />
                {exportMutation.isPending ? t("exporting") : t("exportAction")}
              </button>
              <button
                type="button"
                onClick={handleCopyLink}
                className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
              >
                {copied ? (
                  <>
                    <CheckCircle2 size={14} className="text-green-500" />
                    {t("copied")}
                  </>
                ) : (
                  <>
                    <Copy size={14} />
                    {t("copyLink")}
                  </>
                )}
              </button>
            </PopoverContent>
          </Popover>
        </div>
      </div>

      {isLoading ? (
        <div className="p-8 text-center">
          <div className="mx-auto h-4 w-4 animate-spin rounded-full border-2 border-tennis-green/30 border-t-tennis-green" />
        </div>
      ) : enrollments.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-10 text-center">
          <div className="mb-2 flex h-8 w-8 items-center justify-center rounded-full bg-gray-100">
            <Users size={15} className="text-gray-400" />
          </div>
          <p className="text-sm text-gray-400">{t("empty")}</p>
        </div>
      ) : (
        <EnrollmentsTable enrollments={enrollments} campId={campId} />
      )}
      <CampManualEnrollmentDialog
        campId={campId}
        open={manualOpen}
        onOpenChange={setManualOpen}
      />
    </div>
  );
}
