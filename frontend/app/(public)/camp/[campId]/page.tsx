"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import {
  CalendarDays,
  Clock,
  MapPin,
  User,
  Users,
  Euro,
  Copy,
  Plus,
  X,
} from "lucide-react";
import Link from "next/link";

import {
  getPublicCamp,
  getCampForm,
  submitCampEnrollment,
} from "@/lib/api/camps";
import type {
  PublicCampDto,
  CampEnrollmentFormDto,
  CampFormFieldDto,
} from "@/lib/api/camps";
import { getAuthUser } from "@/lib/auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";
import { Spinner } from "@/components/ui/spinner";
import { formatDateNL, formatDateShort } from "@/lib/date-utils";

// ─── Constants ───────────────────────────────────────────────────────────────

const FIELD_TYPE_MULTIPLE_CHOICE = 2;
const FIELD_TYPE_YES_NO = 3;

const LEVEL_KEYS: Record<number, "level1" | "level2" | "level3"> = {
  1: "level1",
  2: "level2",
  3: "level3",
};

type GroupMember = { name: string; email: string };

// ─── Helpers ─────────────────────────────────────────────────────────────────

function inputClass(hasError: boolean) {
  return `w-full border rounded-lg px-3 py-2 text-sm outline-none transition-colors focus:ring-2 focus:ring-tennis-green/20 ${
    hasError
      ? "border-red-300 focus:border-red-400 bg-red-50"
      : "border-gray-300 focus:border-tennis-green"
  }`;
}

/** Format a full ISO timestamp (registrationDeadline) as dd/MM/yyyy. */
function formatDeadline(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  return `${day}/${month}/${d.getFullYear()}`;
}

// ─── Page ────────────────────────────────────────────────────────────────────

export default function CampEnrollPage() {
  const { campId } = useParams<{ campId: string }>();
  const router = useRouter();
  const t = useTranslations("camps");

  const [camp, setCamp] = useState<PublicCampDto | null>(null);
  const [form, setForm] = useState<CampEnrollmentFormDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  // Form values
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [fieldValues, setFieldValues] = useState<Record<string, string>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [baseErrors, setBaseErrors] = useState<{
    firstName?: string;
    lastName?: string;
    email?: string;
  }>({});

  // Enrollment type
  const [enrollmentType, setEnrollmentType] = useState<"solo" | "group">("solo");
  const [groupMembers, setGroupMembers] = useState<GroupMember[]>([]);
  const [memberErrors, setMemberErrors] = useState<
    Record<number, { name?: string; email?: string }>
  >({});

  const user = getAuthUser();
  const isAdminOrTrainer = user?.role === "Admin" || user?.role === "Trainer";

  useEffect(() => {
    async function loadData() {
      try {
        const [campData, formData] = await Promise.all([
          getPublicCamp(campId),
          getCampForm(campId),
        ]);
        setCamp(campData);
        setForm(formData);
      } catch {
        setError(t("loadDetailError"));
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [campId, t]);

  // ─── Field helpers ──────────────────────────────────────────────────────

  function setFieldValue(fieldId: string, value: string) {
    setFieldValues((prev) => ({ ...prev, [fieldId]: value }));
    if (fieldErrors[fieldId]) {
      setFieldErrors((prev) => {
        const next = { ...prev };
        delete next[fieldId];
        return next;
      });
    }
  }

  function addGroupMember() {
    if (groupMembers.length >= 3) return;
    setGroupMembers((prev) => [...prev, { name: "", email: "" }]);
  }

  function removeGroupMember(index: number) {
    setGroupMembers((prev) => prev.filter((_, i) => i !== index));
    setMemberErrors((prev) => {
      const next = { ...prev };
      delete next[index];
      return next;
    });
  }

  function updateGroupMember(
    index: number,
    field: "name" | "email",
    value: string
  ) {
    setGroupMembers((prev) =>
      prev.map((m, i) => (i === index ? { ...m, [field]: value } : m))
    );
    if (memberErrors[index]?.[field]) {
      setMemberErrors((prev) => {
        const next = { ...prev };
        if (next[index]) next[index] = { ...next[index], [field]: undefined };
        return next;
      });
    }
  }

  // ─── Validation ─────────────────────────────────────────────────────────

  function validate(): boolean {
    const errors: typeof baseErrors = {};
    if (!firstName.trim()) errors.firstName = t("requiredField");
    if (!lastName.trim()) errors.lastName = t("requiredField");
    if (!email.trim()) errors.email = t("requiredField");
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email))
      errors.email = t("requiredField");
    setBaseErrors(errors);

    const fErrors: Record<string, string> = {};
    if (form) {
      for (const field of form.fields) {
        if (field.isRequired && !fieldValues[field.id]?.trim()) {
          fErrors[field.id] = t("requiredField");
        }
      }
    }
    setFieldErrors(fErrors);

    const mErrors: Record<number, { name?: string; email?: string }> = {};
    if (enrollmentType === "group") {
      groupMembers.forEach((m, i) => {
        const e: { name?: string; email?: string } = {};
        if (!m.name.trim()) e.name = t("requiredField");
        if (!m.email.trim()) e.email = t("requiredField");
        else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(m.email))
          e.email = t("requiredField");
        if (e.name || e.email) mErrors[i] = e;
      });
    }
    setMemberErrors(mErrors);

    return (
      Object.keys(errors).length === 0 &&
      Object.keys(fErrors).length === 0 &&
      Object.keys(mErrors).length === 0
    );
  }

  // ─── Submit ─────────────────────────────────────────────────────────────

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    if (!validate()) return;

    setSubmitting(true);
    try {
      const responses = Object.entries(fieldValues)
        .filter(([, v]) => v.trim())
        .map(([campFormFieldId, value]) => ({ campFormFieldId, value }));

      const result = await submitCampEnrollment(campId, {
        participantName: `${firstName.trim()} ${lastName.trim()}`,
        participantEmail: email.trim(),
        participantPhone: phone.trim() || undefined,
        responses,
        enrollmentType,
        groupMembers:
          enrollmentType === "group" && groupMembers.length > 0
            ? groupMembers.map((m) => ({
                participantName: m.name.trim(),
                participantEmail: m.email.trim(),
                responses: [],
              }))
            : undefined,
      });

      if (result.requiresPayment) {
        // Betalend kamp → naar de betaalpagina om online of cash te kiezen.
        router.push(
          `/camp-enrollment/payment?campEnrollmentId=${result.campEnrollmentId}&campId=${campId}`
        );
      } else {
        // Gratis kamp → meteen naar de bevestigingspagina.
        router.push(
          `/camp-enrollment/thank-you?campEnrollmentId=${result.campEnrollmentId}`
        );
      }
    } catch {
      setSubmitError(t("submitError"));
      setSubmitting(false);
    }
  }

  function handleCopyLink() {
    navigator.clipboard.writeText(window.location.href);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  // ─── Custom field renderer ──────────────────────────────────────────────

  function renderCustomField(field: CampFormFieldDto) {
    const value = fieldValues[field.id] ?? "";
    const hasError = !!fieldErrors[field.id];

    if (field.type === FIELD_TYPE_MULTIPLE_CHOICE && field.options) {
      return (
        <div key={field.id} className="space-y-1.5">
          <label className="block text-sm font-medium text-gray-700">
            {field.label}
            {field.isRequired && <span className="text-red-500 ml-1">*</span>}
          </label>
          <div className="space-y-2">
            {field.options.map((opt) => (
              <label key={opt} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name={`field_${field.id}`}
                  value={opt}
                  checked={value === opt}
                  onChange={() => setFieldValue(field.id, opt)}
                  className="w-4 h-4 text-tennis-green focus:ring-tennis-green"
                />
                <span className="text-sm text-gray-700">{opt}</span>
              </label>
            ))}
          </div>
          {hasError && <p className="text-xs text-red-500">{fieldErrors[field.id]}</p>}
        </div>
      );
    }

    if (field.type === FIELD_TYPE_YES_NO) {
      return (
        <div key={field.id} className="space-y-1.5">
          <label className="block text-sm font-medium text-gray-700">
            {field.label}
            {field.isRequired && <span className="text-red-500 ml-1">*</span>}
          </label>
          <div className="flex items-center gap-4">
            {["Ja", "Nee"].map((opt) => (
              <label key={opt} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name={`field_${field.id}`}
                  value={opt}
                  checked={value === opt}
                  onChange={() => setFieldValue(field.id, opt)}
                  className="w-4 h-4 text-tennis-green focus:ring-tennis-green"
                />
                <span className="text-sm text-gray-700">{opt}</span>
              </label>
            ))}
          </div>
          {hasError && <p className="text-xs text-red-500">{fieldErrors[field.id]}</p>}
        </div>
      );
    }

    return (
      <div key={field.id} className="space-y-1.5">
        <label className="block text-sm font-medium text-gray-700">
          {field.label}
          {field.isRequired && <span className="text-red-500 ml-1">*</span>}
        </label>
        <input
          type="text"
          value={value}
          onChange={(e) => setFieldValue(field.id, e.target.value)}
          className={inputClass(hasError)}
        />
        {hasError && <p className="text-xs text-red-500">{fieldErrors[field.id]}</p>}
      </div>
    );
  }

  // ─── Loading / Error ────────────────────────────────────────────────────

  if (loading) {
    return (
      <div className="min-h-screen bg-[#FAFAF8] flex items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (error || !camp) {
    return (
      <div className="min-h-screen bg-[#FAFAF8] flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-500 mb-4">{error ?? t("detailNotFound")}</p>
          <Link href="/" className="text-tennis-green hover:underline text-sm">
            CoachOS
          </Link>
        </div>
      </div>
    );
  }

  const levelKey = camp.level != null ? LEVEL_KEYS[camp.level] : undefined;
  const spotsLeft =
    camp.maxParticipants != null
      ? Math.max(0, camp.maxParticipants - camp.participantCount)
      : null;

  // ─── Render ─────────────────────────────────────────────────────────────

  return (
    <div className="min-h-screen bg-[#FAFAF8]">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Logo */}
        <div className="flex items-center gap-2 mb-8">
          <div className="w-8 h-8 bg-tennis-green rounded-full flex items-center justify-center">
            <TennisBallIcon className="w-4 h-4 text-white" strokeWidth={2} />
          </div>
          <span className="font-semibold text-lg text-tennis-green">CoachOS</span>
        </div>

        {/* Camp info card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          {levelKey && (
            <Badge className="bg-tennis-lime/20 text-tennis-green border-0 mb-2 text-xs font-semibold">
              {t(levelKey)}
            </Badge>
          )}
          <h1 className="text-2xl font-bold text-gray-900 mb-2">{camp.name}</h1>
          {camp.description && (
            <p className="text-gray-600 text-sm mb-4 leading-relaxed">
              {camp.description}
            </p>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <CalendarDays className="w-4 h-4 text-gray-400 shrink-0" />
              <span>
                {formatDateNL(camp.startDate)} tot {formatDateNL(camp.endDate)}
              </span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <MapPin className="w-4 h-4 text-gray-400 shrink-0" />
              <span>{camp.tennisClubName}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Euro className="w-4 h-4 text-gray-400 shrink-0" />
              <span>
                {camp.price > 0 ? `€${camp.price.toFixed(2)}` : t("priceFree")}
              </span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Clock className="w-4 h-4 text-gray-400 shrink-0" />
              <span>
                {t("publicDeadlineLabel")}: {formatDeadline(camp.registrationDeadline)}
              </span>
            </div>
            {spotsLeft != null && (
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <Users className="w-4 h-4 text-gray-400 shrink-0" />
                <span
                  className={
                    spotsLeft === 0
                      ? "text-red-600 font-medium"
                      : spotsLeft <= (camp.maxParticipants ?? 0) * 0.2
                        ? "text-amber-600 font-medium"
                        : "text-tennis-green font-medium"
                  }
                >
                  {t("spotsLeft", { count: spotsLeft })}
                </span>
              </div>
            )}
          </div>

          {/* Programma — per dag datum + uren */}
          {camp.days.length > 0 && (
            <div className="mt-5 pt-5 border-t border-gray-100">
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
                {t("publicDaysLabel")}
              </h3>
              <div className="space-y-2">
                {camp.days.map((day) => (
                  <div
                    key={day.id}
                    className="flex items-center justify-between text-sm border border-gray-100 rounded-lg px-3 py-2"
                  >
                    <span className="font-medium text-gray-900 capitalize">
                      {formatDateShort(day.date)}
                    </span>
                    <span className="text-gray-600">
                      {day.startTime} - {day.endTime}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Enrollment form card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <form onSubmit={handleSubmit} noValidate>
            {/* Form header */}
            <div className="px-6 py-5 border-b border-gray-100">
              <h2 className="text-lg font-semibold text-gray-900">
                {t("publicTitle")}
              </h2>
              <p className="text-sm text-gray-500 mt-0.5">{t("publicIntro")}</p>
            </div>

            <div className="p-6 space-y-6">
              {submitError && (
                <div className="px-4 py-3 rounded-lg bg-red-50 border border-red-100">
                  <p className="text-red-600 text-sm">{submitError}</p>
                </div>
              )}

              {/* ── Personal info ── */}
              <div>
                <div className="space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1.5">
                        {t("formFirstName")} *
                      </label>
                      <input
                        type="text"
                        value={firstName}
                        onChange={(e) => {
                          setFirstName(e.target.value);
                          setBaseErrors((p) => ({ ...p, firstName: undefined }));
                        }}
                        className={inputClass(!!baseErrors.firstName)}
                      />
                      {baseErrors.firstName && (
                        <p className="text-xs text-red-500 mt-1">{baseErrors.firstName}</p>
                      )}
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1.5">
                        {t("formLastName")} *
                      </label>
                      <input
                        type="text"
                        value={lastName}
                        onChange={(e) => {
                          setLastName(e.target.value);
                          setBaseErrors((p) => ({ ...p, lastName: undefined }));
                        }}
                        className={inputClass(!!baseErrors.lastName)}
                      />
                      {baseErrors.lastName && (
                        <p className="text-xs text-red-500 mt-1">{baseErrors.lastName}</p>
                      )}
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1.5">
                      {t("formEmail")} *
                    </label>
                    <input
                      type="email"
                      value={email}
                      onChange={(e) => {
                        setEmail(e.target.value);
                        setBaseErrors((p) => ({ ...p, email: undefined }));
                      }}
                      className={inputClass(!!baseErrors.email)}
                    />
                    {baseErrors.email && (
                      <p className="text-xs text-red-500 mt-1">{baseErrors.email}</p>
                    )}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1.5">
                      {t("formPhoneOptional")}
                    </label>
                    <input
                      type="tel"
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      placeholder="+32 471 23 45 67"
                      className={inputClass(false)}
                    />
                  </div>
                </div>
              </div>

              <hr className="border-gray-100" />

              {/* ── Enrollment type ── */}
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
                  {t("enrollmentType")}
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <label
                    className={`border-2 rounded-lg p-4 cursor-pointer transition ${
                      enrollmentType === "solo"
                        ? "border-tennis-green bg-tennis-green/5"
                        : "border-gray-200 hover:border-tennis-green/30"
                    }`}
                  >
                    <input
                      type="radio"
                      name="enrollType"
                      value="solo"
                      checked={enrollmentType === "solo"}
                      onChange={() => setEnrollmentType("solo")}
                      className="sr-only"
                    />
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center shrink-0">
                        <User className="w-5 h-5 text-gray-600" />
                      </div>
                      <div className="font-medium text-gray-900 text-sm">
                        {t("typeSolo")}
                      </div>
                    </div>
                  </label>

                  <label
                    className={`border-2 rounded-lg p-4 cursor-pointer transition ${
                      enrollmentType === "group"
                        ? "border-tennis-green bg-tennis-green/5"
                        : "border-gray-200 hover:border-tennis-green/30"
                    }`}
                  >
                    <input
                      type="radio"
                      name="enrollType"
                      value="group"
                      checked={enrollmentType === "group"}
                      onChange={() => setEnrollmentType("group")}
                      className="sr-only"
                    />
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center shrink-0">
                        <Users className="w-5 h-5 text-gray-600" />
                      </div>
                      <div className="font-medium text-gray-900 text-sm">
                        {t("typeGroup")}
                      </div>
                    </div>
                  </label>
                </div>

                {/* Group: member fields */}
                {enrollmentType === "group" && (
                  <div className="mt-4 space-y-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-gray-700">
                        {t("groupMembersTitle")}
                      </span>
                      {groupMembers.length < 3 && (
                        <button
                          type="button"
                          onClick={addGroupMember}
                          className="text-sm text-tennis-green hover:underline font-medium flex items-center gap-1"
                        >
                          <Plus size={14} />
                          {t("addMember")}
                        </button>
                      )}
                    </div>

                    {groupMembers.map((member, i) => (
                      <div
                        key={i}
                        className="border border-gray-200 rounded-lg p-3 space-y-3"
                      >
                        <div className="flex items-center justify-end">
                          <button
                            type="button"
                            onClick={() => removeGroupMember(i)}
                            className="text-xs text-gray-400 hover:text-red-500 flex items-center gap-1 transition-colors"
                          >
                            <X size={12} />
                            {t("removeMember")}
                          </button>
                        </div>
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                          <div>
                            <input
                              type="text"
                              value={member.name}
                              onChange={(e) =>
                                updateGroupMember(i, "name", e.target.value)
                              }
                              placeholder={t("memberName")}
                              className={inputClass(!!memberErrors[i]?.name)}
                            />
                            {memberErrors[i]?.name && (
                              <p className="text-xs text-red-500 mt-1">
                                {memberErrors[i].name}
                              </p>
                            )}
                          </div>
                          <div>
                            <input
                              type="email"
                              value={member.email}
                              onChange={(e) =>
                                updateGroupMember(i, "email", e.target.value)
                              }
                              placeholder={t("memberEmail")}
                              className={inputClass(!!memberErrors[i]?.email)}
                            />
                            {memberErrors[i]?.email && (
                              <p className="text-xs text-red-500 mt-1">
                                {memberErrors[i].email}
                              </p>
                            )}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* ── Custom form fields ── */}
              {form && form.fields.length > 0 && (
                <>
                  <hr className="border-gray-100" />
                  <div>
                    <div className="space-y-4">
                      {form.fields.map((field) => renderCustomField(field))}
                    </div>
                  </div>
                </>
              )}
            </div>

            {/* Submit footer */}
            <div className="px-6 py-5 bg-gray-50 border-t border-gray-100">
              <button
                type="submit"
                disabled={submitting}
                className="w-full bg-tennis-green text-white py-3 rounded-lg font-medium hover:bg-tennis-green/90 transition text-sm disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {submitting && <Spinner />}
                {submitting ? t("submitting") : t("submit")}
              </button>
            </div>
          </form>

          {/* Share link for admin/trainer */}
          {isAdminOrTrainer && (
            <div className="px-6 py-4 border-t border-gray-100">
              <Button
                variant="outline"
                className="w-full h-11 border-gray-200 cursor-pointer"
                onClick={handleCopyLink}
              >
                <Copy className="w-4 h-4 mr-2" />
                {copied ? t("linkCopied") : t("shareLink")}
              </Button>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="text-center mt-8 text-xs text-gray-400">
          Powered by CoachOS
        </div>
      </div>
    </div>
  );
}
