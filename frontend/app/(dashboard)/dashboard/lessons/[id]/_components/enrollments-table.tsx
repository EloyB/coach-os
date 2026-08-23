"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import {
  Search,
  ChevronDown,
  ChevronRight,
  ChevronUp,
  Pencil,
  Trash2,
  Euro,
  Users,
  User,
  Copy,
  CheckCircle2,
  MoreVertical,
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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";
import { enrollmentStatusStyles } from "@/lib/status-styles";
import {
  getLessonSeriesEnrollments,
  cancelEnrollment,
  markEnrollmentCashPaid,
  updateBasicEnrollment,
} from "@/lib/api/enrollments";
import type { LessonSeriesEnrollmentDto } from "@/lib/api/enrollments";

// ─── Helpers ─────────────────────────────────────────────────────────────────

/** Leeftijd in hele jaren op vandaag; null wanneer geen (geldige) geboortedatum. */
function computeAge(dob: string | null): number | null {
  if (!dob) return null;
  const b = new Date(dob + "T00:00:00");
  if (Number.isNaN(b.getTime())) return null;
  const now = new Date();
  let age = now.getFullYear() - b.getFullYear();
  const m = now.getMonth() - b.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < b.getDate())) age--;
  return age;
}

function formatEnrolledAt(value: string): string {
  return new Date(value).toLocaleDateString("nl-BE", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

type Block =
  | { kind: "solo"; enrollment: LessonSeriesEnrollmentDto }
  | {
      kind: "group";
      groupId: string;
      leader: LessonSeriesEnrollmentDto;
      members: LessonSeriesEnrollmentDto[];
    };

/**
 * Groepeert inschrijvingen in solo-rijen en groep-blokken. De leider staat
 * bovenaan de leden; blokken worden alfabetisch gesorteerd (groepen op leidernaam).
 */
function buildBlocks(enrollments: LessonSeriesEnrollmentDto[]): Block[] {
  const byGroup = new Map<string, LessonSeriesEnrollmentDto[]>();
  const solos: LessonSeriesEnrollmentDto[] = [];

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
      return a.studentName.localeCompare(b.studentName);
    });
    blocks.push({ kind: "group", groupId, leader, members: sortedMembers });
  }

  for (const enrollment of solos) blocks.push({ kind: "solo", enrollment });

  return blocks.sort((a, b) => displayName(a).localeCompare(displayName(b)));
}

function displayName(block: Block): string {
  return block.kind === "group"
    ? block.leader.studentName
    : block.enrollment.studentName;
}

function contactLine(
  enrollment: LessonSeriesEnrollmentDto,
  viaLabel: (email: string) => string,
): string {
  const primary = enrollment.hasOwnEmail
    ? (enrollment.studentEmail ?? "")
    : viaLabel(enrollment.contactEmail);
  return enrollment.studentPhone
    ? `${primary} · ${enrollment.studentPhone}`
    : primary;
}

// ─── Column count (voor colSpan) ───────────────────────────────────────────────

const COLS = 6;

// ─── Section header row (Groepen / Individueel) ─────────────────────────────────

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

// ─── Person row (solo of groepslid) ────────────────────────────────────────────

function PersonRow({
  enrollment,
  seriesId,
  isMember,
  isLeader,
  isDuplicate,
  isMatch,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  isMember: boolean;
  isLeader: boolean;
  isDuplicate: boolean;
  isMatch: boolean;
}) {
  const t = useTranslations("enrollmentsTable");
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [editing, setEditing] = useState(false);
  const [showActionsMenu, setShowActionsMenu] = useState(false);
  const [confirmCancelOpen, setConfirmCancelOpen] = useState(false);

  const hasResponses = enrollment.formResponses.length > 0;
  const isCancelled = enrollment.status === "Cancelled";
  const isPendingPayment = enrollment.status === "PendingPayment";
  const ownsPayment = enrollment.enrollmentGroupId == null || enrollment.isGroupLeader;
  const age = computeAge(enrollment.dateOfBirth);

  const cancelMutation = useMutation({
    mutationFn: () => cancelEnrollment(seriesId, enrollment.id),
    onSuccess: () => {
      toast.success("Inschrijving geannuleerd");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
    },
  });

  const markPaidMutation = useMutation({
    mutationFn: () => markEnrollmentCashPaid(enrollment.id),
    onSuccess: () => {
      toast.success("Inschrijving gemarkeerd als betaald");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
    },
  });

  return (
    <>
      <tr
        className={`border-t border-gray-50 ${
          isCancelled ? "opacity-50" : "hover:bg-gray-50/60 cursor-pointer"
        } ${isMatch ? "bg-tennis-lime/10" : ""}`}
        onClick={() => !isCancelled && setEditing(true)}
      >
        {/* Naam */}
        <td className={`px-4 py-2.5 ${isMember ? "pl-10" : ""}`}>
          <div className="flex items-center gap-2 min-w-0">
            {isMember && (
              <User size={13} className="shrink-0 text-gray-300" />
            )}
            <span
              className={`text-sm font-medium text-gray-800 truncate ${
                isCancelled ? "line-through" : ""
              }`}
            >
              {enrollment.studentName}
            </span>
            {isLeader && (
              <span className="shrink-0 rounded bg-tennis-green/10 px-1.5 py-0.5 text-[10px] font-semibold text-tennis-green">
                {t("leader")}
              </span>
            )}
            {isDuplicate && (
              <Badge className="shrink-0 border-0 bg-amber-100 text-amber-700 text-[10px]">
                {t("possibleDuplicate")}
              </Badge>
            )}
          </div>
        </td>

        {/* Contact */}
        <td className="px-4 py-2.5">
          <span className="block max-w-[220px] truncate text-xs text-gray-500">
            {contactLine(enrollment, (email) => t("viaContact", { email }))}
          </span>
        </td>

        {/* Leeftijd */}
        <td className="px-4 py-2.5 text-xs text-gray-600 whitespace-nowrap">
          {age != null ? t("ageYears", { count: age }) : t("unknown")}
        </td>

        {/* Ingeschreven */}
        <td className="px-4 py-2.5 text-xs text-gray-500 whitespace-nowrap">
          {formatEnrolledAt(enrollment.enrolledAt)}
        </td>

        {/* Status */}
        <td className="px-4 py-2.5 whitespace-nowrap">
          <div className="flex items-center gap-2">
            {enrollmentStatusStyles[enrollment.status] && (
              <Badge
                className={`${enrollmentStatusStyles[enrollment.status].className} border-0 text-xs`}
              >
                {enrollmentStatusStyles[enrollment.status].label}
              </Badge>
            )}
            {isPendingPayment && ownsPayment && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  markPaidMutation.mutate();
                }}
                disabled={markPaidMutation.isPending}
                className="flex items-center gap-1 rounded-md border border-tennis-green/20 px-2 py-1 text-[11px] font-medium text-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
              >
                <Euro size={11} />
                {t("markPaid")}
              </button>
            )}
          </div>
        </td>

        {/* Acties */}
        <td className="px-4 py-2.5 text-right whitespace-nowrap">
          <div className="relative inline-block">
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setShowActionsMenu((v) => !v);
              }}
              aria-label={t("actionsLabel", { name: enrollment.studentName })}
              className="flex h-8 w-8 items-center justify-center rounded-md border border-gray-100 text-gray-400 hover:bg-gray-50 hover:text-gray-700"
            >
              <MoreVertical size={15} />
            </button>
            {showActionsMenu && (
              <div
                onClick={(e) => e.stopPropagation()}
                className="absolute right-0 top-full z-50 mt-1 min-w-48 rounded-lg border border-gray-100 bg-white py-1 text-sm shadow-lg"
              >
                {!isCancelled && (
                  <button
                    type="button"
                    onClick={() => {
                      setShowActionsMenu(false);
                      setEditing(true);
                    }}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-gray-700 hover:bg-tennis-green/5 hover:text-tennis-green"
                  >
                    <Pencil size={13} />
                    {t("editAction")}
                  </button>
                )}
                {hasResponses && (
                  <button
                    type="button"
                    onClick={() => {
                      setShowActionsMenu(false);
                      setExpanded((v) => !v);
                    }}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-gray-700 hover:bg-gray-50"
                  >
                    {expanded ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
                    {expanded ? t("detailsHide") : t("detailsShow")}
                  </button>
                )}
                {!isCancelled && (
                  <button
                    type="button"
                    disabled={cancelMutation.isPending}
                    onClick={() => {
                      setShowActionsMenu(false);
                      setConfirmCancelOpen(true);
                    }}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-red-600 hover:bg-red-50 disabled:opacity-50"
                  >
                    <Trash2 size={13} />
                    {t("cancelAction")}
                  </button>
                )}
              </div>
            )}
          </div>
        </td>
      </tr>

      {expanded && hasResponses && (
        <tr className={isMatch ? "bg-tennis-lime/10" : ""}>
          <td colSpan={COLS} className={`px-4 pb-3 ${isMember ? "pl-10" : ""}`}>
            <dl className="space-y-1.5 rounded-lg bg-[#FAFAF8] p-3">
              {enrollment.formResponses.map((r, i) => (
                <div key={i} className="flex gap-3 text-xs">
                  <dt className="min-w-[120px] shrink-0 text-gray-500">
                    {r.fieldLabel}
                  </dt>
                  <dd className="font-medium text-gray-800">{r.value}</dd>
                </div>
              ))}
            </dl>
          </td>
        </tr>
      )}

      <EditEnrollmentDialog
        enrollment={enrollment}
        seriesId={seriesId}
        open={editing}
        onOpenChange={setEditing}
      />

      <AlertDialog open={confirmCancelOpen} onOpenChange={setConfirmCancelOpen}>
        <AlertDialogContent onClick={(e) => e.stopPropagation()}>
          <AlertDialogHeader>
            <AlertDialogTitle>Inschrijving annuleren?</AlertDialogTitle>
            <AlertDialogDescription>
              De inschrijving van {enrollment.studentName} wordt op geannuleerd
              gezet en de plaats komt weer vrij. De formulierantwoorden blijven
              bewaard.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Terug</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => cancelMutation.mutate()}
              className="bg-red-600 hover:bg-red-700"
            >
              Annuleren
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

// ─── Group block (inklapbare kop + ledenrijen) ─────────────────────────────────

function GroupBlockRows({
  block,
  seriesId,
  forceExpanded,
  duplicateIds,
  matchedIds,
}: {
  block: Extract<Block, { kind: "group" }>;
  seriesId: string;
  forceExpanded: boolean;
  duplicateIds: Set<string>;
  matchedIds: Set<string> | null;
}) {
  const t = useTranslations("enrollmentsTable");
  const [open, setOpen] = useState(false);
  const expanded = forceExpanded || open;

  const { leader, members } = block;
  const isPendingPayment = leader.status === "PendingPayment";

  return (
    <>
      <tr
        className="border-t border-gray-100 bg-gray-50/40 cursor-pointer hover:bg-gray-50"
        onClick={() => setOpen((v) => !v)}
      >
        {/* Naam / groepskop */}
        <td className="px-4 py-2.5" colSpan={2}>
          <div className="flex items-center gap-2 min-w-0">
            {expanded ? (
              <ChevronDown size={15} className="shrink-0 text-gray-400" />
            ) : (
              <ChevronRight size={15} className="shrink-0 text-gray-400" />
            )}
            <Users size={14} className="shrink-0 text-tennis-green" />
            <span className="truncate text-sm font-semibold text-gray-800">
              {t("group")} · {leader.studentName}
            </span>
            <Badge className="shrink-0 border-0 bg-tennis-green/10 text-tennis-green text-[11px]">
              {t("members", { count: members.length })}
            </Badge>
          </div>
        </td>

        {/* Leeftijd (leeg voor kop) */}
        <td className="px-4 py-2.5" />

        {/* Ingeschreven */}
        <td className="px-4 py-2.5 text-xs text-gray-500 whitespace-nowrap">
          {formatEnrolledAt(leader.enrolledAt)}
        </td>

        {/* Status (leider) + markeer betaald */}
        <td className="px-4 py-2.5 whitespace-nowrap">
          <div className="flex items-center gap-2">
            {enrollmentStatusStyles[leader.status] && (
              <Badge
                className={`${enrollmentStatusStyles[leader.status].className} border-0 text-xs`}
              >
                {enrollmentStatusStyles[leader.status].label}
              </Badge>
            )}
            {isPendingPayment && (
              <MarkGroupPaidButton seriesId={seriesId} enrollmentId={leader.id} />
            )}
          </div>
        </td>

        {/* Acties (leeg voor kop — acties zitten per lid) */}
        <td className="px-4 py-2.5" />
      </tr>

      {expanded &&
        members.map((m) => (
          <PersonRow
            key={m.id}
            enrollment={m}
            seriesId={seriesId}
            isMember
            isLeader={m.id === leader.id}
            isDuplicate={duplicateIds.has(m.id)}
            isMatch={matchedIds?.has(m.id) ?? false}
          />
        ))}
    </>
  );
}

function MarkGroupPaidButton({
  seriesId,
  enrollmentId,
}: {
  seriesId: string;
  enrollmentId: string;
}) {
  const t = useTranslations("enrollmentsTable");
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => markEnrollmentCashPaid(enrollmentId),
    onSuccess: () => {
      toast.success("Inschrijving gemarkeerd als betaald");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
    },
  });
  return (
    <button
      onClick={(e) => {
        e.stopPropagation();
        mutation.mutate();
      }}
      disabled={mutation.isPending}
      className="flex items-center gap-1 rounded-md border border-tennis-green/20 px-2 py-1 text-[11px] font-medium text-tennis-green hover:bg-tennis-green/5 disabled:opacity-50"
    >
      <Euro size={11} />
      {t("markPaid")}
    </button>
  );
}

// ─── Table ─────────────────────────────────────────────────────────────────────

function EnrollmentsTable({
  enrollments,
  seriesId,
}: {
  enrollments: LessonSeriesEnrollmentDto[];
  seriesId: string;
}) {
  const t = useTranslations("enrollmentsTable");
  const [query, setQuery] = useState("");
  const [showCancelled, setShowCancelled] = useState(false);

  const cancelledCount = useMemo(
    () => enrollments.filter((e) => e.status === "Cancelled").length,
    [enrollments],
  );

  // Mogelijke dubbels: zelfde contactadres + genormaliseerde naam > 1x.
  const duplicateIds = useMemo(() => {
    const keyed = enrollments.map((e) => ({
      id: e.id,
      key: `${e.contactEmail}|${e.studentName.trim().toLowerCase()}`,
    }));
    return new Set(
      keyed
        .filter((row) => keyed.filter((o) => o.key === row.key).length > 1)
        .map((row) => row.id),
    );
  }, [enrollments]);

  const visible = useMemo(
    () => (showCancelled ? enrollments : enrollments.filter((e) => e.status !== "Cancelled")),
    [enrollments, showCancelled],
  );

  const blocks = useMemo(() => buildBlocks(visible), [visible]);

  const q = query.trim().toLowerCase();
  const matches = (e: LessonSeriesEnrollmentDto) =>
    e.studentName.toLowerCase().includes(q);

  // Bij zoeken: enkel matchende blokken; groepen uitgeklapt; matchende leden gehighlight.
  const { filteredBlocks, matchedIds } = useMemo(() => {
    if (!q) return { filteredBlocks: blocks, matchedIds: null as Set<string> | null };
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blocks, q]);

  const groupBlocks = filteredBlocks.filter(
    (b): b is Extract<Block, { kind: "group" }> => b.kind === "group",
  );
  const soloBlocks = filteredBlocks.filter(
    (b): b is Extract<Block, { kind: "solo" }> => b.kind === "solo",
  );

  return (
    <div>
      {/* Zoekveld */}
      <div className="flex items-center gap-3 px-5 py-3 border-b border-gray-100">
        <div className="relative flex-1 max-w-xs">
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
            {showCancelled ? t("hideCancelled") : t("showCancelled", { count: cancelledCount })}
          </button>
        )}
      </div>

      {filteredBlocks.length === 0 ? (
        <div className="py-10 text-center text-sm text-gray-400">
          {q ? t("noResults", { query }) : t("empty")}
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-left">
            <thead>
              <tr className="border-b border-gray-100 text-[11px] font-semibold uppercase tracking-wide text-gray-400">
                <th className="px-4 py-2 font-semibold">{t("colName")}</th>
                <th className="px-4 py-2 font-semibold">{t("colContact")}</th>
                <th className="px-4 py-2 font-semibold">{t("colAge")}</th>
                <th className="px-4 py-2 font-semibold">{t("colEnrolled")}</th>
                <th className="px-4 py-2 font-semibold">{t("colStatus")}</th>
                <th className="px-4 py-2" />
              </tr>
            </thead>
            <tbody>
              {groupBlocks.length > 0 && (
                <SectionHeaderRow label={t("sectionGroups")} count={groupBlocks.length} />
              )}
              {groupBlocks.map((block) => (
                <GroupBlockRows
                  key={block.groupId}
                  block={block}
                  seriesId={seriesId}
                  forceExpanded={!!q}
                  duplicateIds={duplicateIds}
                  matchedIds={matchedIds}
                />
              ))}

              {soloBlocks.length > 0 && (
                <SectionHeaderRow label={t("sectionIndividual")} count={soloBlocks.length} />
              )}
              {soloBlocks.map((block) => (
                <PersonRow
                  key={block.enrollment.id}
                  enrollment={block.enrollment}
                  seriesId={seriesId}
                  isMember={false}
                  isLeader={false}
                  isDuplicate={duplicateIds.has(block.enrollment.id)}
                  isMatch={matchedIds?.has(block.enrollment.id) ?? false}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ─── Section (fetch + kop + tabel) ─────────────────────────────────────────────

export function EnrollmentsSection({ seriesId }: { seriesId: string }) {
  const t = useTranslations("enrollmentsTable");
  const [copied, setCopied] = useState(false);

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ["enrollments", seriesId],
    queryFn: () => getLessonSeriesEnrollments(seriesId),
  });

  const activeCount = enrollments.filter(
    (e) => e.status === "Confirmed" || e.status === "Pending",
  ).length;

  function handleCopyLink() {
    const url = `${window.location.origin}/enroll/${seriesId}`;
    navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div
      id="enrollments"
      className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-visible scroll-mt-20"
    >
      <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <h2 className="text-sm font-semibold text-gray-800">{t("title")}</h2>
          <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-tennis-green/10 text-xs font-bold text-tennis-green">
            {activeCount}
          </span>
        </div>
        <button
          onClick={handleCopyLink}
          className="flex items-center gap-1.5 rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-50"
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
        <EnrollmentsTable enrollments={enrollments} seriesId={seriesId} />
      )}
    </div>
  );
}

// ─── Edit dialog (verplaatst uit page.tsx) ─────────────────────────────────────

const basicEnrollmentSchema = z.object({
  studentName: z.string().min(1, "Naam is verplicht"),
  contactEmail: z.string().email("Ongeldig e-mailadres"),
  studentEmail: z.string().email("Ongeldig e-mailadres").or(z.literal("")),
  studentPhone: z.string(),
  dateOfBirth: z.string().min(1, "Geboortedatum is verplicht"),
  isOpenToGrouping: z.boolean(),
});

type BasicEnrollmentFormValues = z.infer<typeof basicEnrollmentSchema>;

function EditEnrollmentDialog({
  enrollment,
  seriesId,
  open,
  onOpenChange,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const queryClient = useQueryClient();
  const form = useForm<BasicEnrollmentFormValues>({
    resolver: zodResolver(basicEnrollmentSchema),
    values: {
      studentName: enrollment.studentName,
      contactEmail: enrollment.contactEmail,
      studentEmail: enrollment.studentEmail ?? "",
      studentPhone: enrollment.studentPhone ?? "",
      dateOfBirth: enrollment.dateOfBirth ?? "",
      isOpenToGrouping: enrollment.isOpenToGrouping,
    },
  });

  const mutation = useMutation({
    mutationFn: (values: BasicEnrollmentFormValues) =>
      updateBasicEnrollment(seriesId, enrollment.id, {
        studentName: values.studentName,
        contactEmail: values.contactEmail,
        studentEmail: values.studentEmail?.trim() ? values.studentEmail : null,
        studentPhone: values.studentPhone?.trim() ? values.studentPhone : null,
        dateOfBirth: values.dateOfBirth,
        isOpenToGrouping: values.isOpenToGrouping,
      }),
    onSuccess: () => {
      toast.success("Inschrijving bijgewerkt");
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["planning", seriesId] });
      onOpenChange(false);
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onClick={(e) => e.stopPropagation()} className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Inschrijving aanpassen</DialogTitle>
          <DialogDescription>
            Wijzig enkel basisgegevens. Betaling en planning blijven ongewijzigd.
          </DialogDescription>
        </DialogHeader>
        <p className="text-xs text-gray-500">
          Ingeschreven op {new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
        </p>
        <form
          onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
          className="space-y-4"
        >
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Naam deelnemer
              </label>
              <input className={inputClass} {...form.register("studentName")} />
              <FieldError message={form.formState.errors.studentName?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Contact e-mail
              </label>
              <input type="email" className={inputClass} {...form.register("contactEmail")} />
              <FieldError message={form.formState.errors.contactEmail?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Eigen e-mail deelnemer
              </label>
              <input type="email" className={inputClass} {...form.register("studentEmail")} />
              <FieldError message={form.formState.errors.studentEmail?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">Telefoon</label>
              <input className={inputClass} {...form.register("studentPhone")} />
              <FieldError message={form.formState.errors.studentPhone?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Geboortedatum
              </label>
              <input type="date" className={inputClass} {...form.register("dateOfBirth")} />
              <FieldError message={form.formState.errors.dateOfBirth?.message} />
            </div>
          </div>
          <label className="flex items-center gap-2 text-xs text-gray-600">
            <input type="checkbox" {...form.register("isOpenToGrouping")} />
            Open voor groepering met andere deelnemers
          </label>
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
            Deze wijziging past geen betaalstatus, betalingsbedrag of planningstoewijzing aan.
          </p>
          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className="rounded-lg border border-gray-200 px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
            >
              Annuleren
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="rounded-lg bg-tennis-green px-3 py-2 text-sm font-medium text-white hover:bg-tennis-green/90 disabled:opacity-50"
            >
              {mutation.isPending ? "Opslaan…" : "Opslaan"}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
