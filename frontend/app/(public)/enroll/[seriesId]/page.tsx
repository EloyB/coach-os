"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import {
  CalendarDays,
  Clock,
  MapPin,
  User,
  Users,
  Euro,
  CheckCircle2,
  Copy,
  Plus,
  X,
  Check,
} from "lucide-react";

import {
  getPublicLessonSeries,
  getEnrollmentForm,
  submitEnrollment,
} from "@/lib/api/enrollments";
import type {
  PublicLessonSeriesDto,
  EnrollmentFormDto,
  FormFieldDto,
} from "@/lib/api/enrollments";
import { getPublicTimeSlots } from "@/lib/api/timeSlots";
import type { TimeSlotDto } from "@/lib/api/timeSlots";
import { LESSON_LEVELS } from "@/lib/api/lessonSeries";
import { getAuthUser } from "@/lib/auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";
import { Spinner } from "@/components/ui/spinner";
import { formatDateNL } from "@/lib/date-utils";
import Link from "next/link";

// ─── Constants ───────────────────────────────────────────────────────────────

const FIELD_TYPE_TEXT = 1;
const FIELD_TYPE_MULTIPLE_CHOICE = 2;
const FIELD_TYPE_YES_NO = 3;
const FIELD_TYPE_AGE_CATEGORY = 4;

const PREF_AVAILABLE = 1;
const PREF_PREFERRED = 2;
const PREF_UNAVAILABLE = 3;

const DAY_NAMES = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

type GroupMember = { name: string; email: string };

// ─── Helpers ─────────────────────────────────────────────────────────────────

function inputClass(hasError: boolean) {
  return `w-full border rounded-lg px-3 py-2 text-sm outline-none transition-colors focus:ring-2 focus:ring-tennis-green/20 ${
    hasError
      ? "border-red-300 focus:border-red-400 bg-red-50"
      : "border-gray-300 focus:border-tennis-green"
  }`;
}

// ─── Page ────────────────────────────────────────────────────────────────────

export default function EnrollPage() {
  const { seriesId } = useParams<{ seriesId: string }>();
  const t = useTranslations("enrollments");

  const [series, setSeries] = useState<PublicLessonSeriesDto | null>(null);
  const [form, setForm] = useState<EnrollmentFormDto | null>(null);
  const [timeSlots, setTimeSlots] = useState<TimeSlotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
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

  // Availability preferences
  const [preferences, setPreferences] = useState<Record<string, number>>({});

  // Enrollment type
  const [enrollmentType, setEnrollmentType] = useState<"solo" | "group">(
    "solo"
  );
  const [isOpenToGrouping, setIsOpenToGrouping] = useState(false);
  const [groupMembers, setGroupMembers] = useState<GroupMember[]>([]);
  const [memberErrors, setMemberErrors] = useState<
    Record<number, { name?: string; email?: string }>
  >({});

  const user = getAuthUser();
  const isAdminOrTrainer = user?.role === "Admin" || user?.role === "Trainer";

  useEffect(() => {
    async function loadData() {
      try {
        const [seriesData, formData, slotsData] = await Promise.all([
          getPublicLessonSeries(seriesId),
          getEnrollmentForm(seriesId),
          getPublicTimeSlots(seriesId),
        ]);
        setSeries(seriesData);
        setForm(formData);
        setTimeSlots(slotsData);
      } catch {
        setError("Lessenreeks niet gevonden.");
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [seriesId]);

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

  function setPreference(slotId: string, pref: number) {
    setPreferences((prev) => ({ ...prev, [slotId]: pref }));
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
    if (!firstName.trim()) errors.firstName = "Voornaam is verplicht";
    if (!lastName.trim()) errors.lastName = "Achternaam is verplicht";
    if (!email.trim()) errors.email = "E-mailadres is verplicht";
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email))
      errors.email = "Ongeldig e-mailadres";
    setBaseErrors(errors);

    const fErrors: Record<string, string> = {};
    if (form) {
      for (const field of form.fields) {
        if (field.isRequired && !fieldValues[field.id]?.trim()) {
          fErrors[field.id] = `${field.label} is verplicht`;
        }
      }
    }
    setFieldErrors(fErrors);

    const mErrors: Record<number, { name?: string; email?: string }> = {};
    if (enrollmentType === "group") {
      groupMembers.forEach((m, i) => {
        const e: { name?: string; email?: string } = {};
        if (!m.name.trim()) e.name = "Naam is verplicht";
        if (!m.email.trim()) e.email = "E-mailadres is verplicht";
        else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(m.email))
          e.email = "Ongeldig e-mailadres";
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
        .map(([formFieldId, value]) => ({ formFieldId, value }));

      const timeSlotPreferences = Object.entries(preferences).map(
        ([weeklyTemplateEntryId, preference]) => ({
          weeklyTemplateEntryId,
          preference,
        })
      );

      await submitEnrollment(seriesId, {
        studentName: `${firstName.trim()} ${lastName.trim()}`,
        studentEmail: email.trim(),
        studentPhone: phone.trim() || undefined,
        responses,
        timeSlotPreferences:
          timeSlotPreferences.length > 0 ? timeSlotPreferences : undefined,
        enrollmentType,
        isOpenToGrouping,
        groupMembers:
          enrollmentType === "group" && groupMembers.length > 0
            ? groupMembers.map((m) => ({
                studentName: m.name.trim(),
                studentEmail: m.email.trim(),
                responses: [],
              }))
            : undefined,
      });
      setSubmitted(true);
    } catch {
      setSubmitError(t("form_error"));
    } finally {
      setSubmitting(false);
    }
  }

  function handleCopyLink() {
    navigator.clipboard.writeText(window.location.href);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  // ─── Custom field renderer ──────────────────────────────────────────────

  function renderCustomField(field: FormFieldDto) {
    const value = fieldValues[field.id] ?? "";
    const hasError = !!fieldErrors[field.id];

    if (
      (field.type === FIELD_TYPE_MULTIPLE_CHOICE ||
        field.type === FIELD_TYPE_AGE_CATEGORY) &&
      field.options
    ) {
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

  // ─── Preference button component ────────────────────────────────────────

  function PrefButton({
    slotId,
    value,
    color,
    icon,
  }: {
    slotId: string;
    value: number;
    color: { border: string; bg: string };
    icon: "check" | "x";
  }) {
    const isSelected = preferences[slotId] === value;
    return (
      <label className="cursor-pointer">
        <input
          type="radio"
          name={`pref_${slotId}`}
          checked={isSelected}
          onChange={() => setPreference(slotId, value)}
          className="sr-only peer"
        />
        <div
          className="w-8 h-8 rounded-full border-2 flex items-center justify-center transition-colors"
          style={{
            borderColor: isSelected ? color.border : "#e5e7eb",
            backgroundColor: isSelected ? color.bg : "transparent",
          }}
        >
          {icon === "check" ? (
            <Check
              size={16}
              strokeWidth={3}
              className={isSelected ? "text-white" : "text-transparent"}
            />
          ) : (
            <X
              size={16}
              strokeWidth={3}
              className={isSelected ? "text-white" : "text-transparent"}
            />
          )}
        </div>
      </label>
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

  if (error || !series) {
    return (
      <div className="min-h-screen bg-[#FAFAF8] flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-500 mb-4">{error ?? "Lessenreeks niet gevonden."}</p>
          <Link href="/" className="text-tennis-green hover:underline text-sm">
            Terug naar home
          </Link>
        </div>
      </div>
    );
  }

  const sortedSlots = [...timeSlots].sort(
    (a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime)
  );

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

        {/* Series info card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          <Badge className="bg-tennis-lime/20 text-tennis-green border-0 mb-2 text-xs font-semibold">
            {LESSON_LEVELS[series.level] ?? "Niveau " + series.level}
          </Badge>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">{series.name}</h1>
          {series.description && (
            <p className="text-gray-600 text-sm mb-4 leading-relaxed">
              {series.description}
            </p>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <CalendarDays className="w-4 h-4 text-gray-400 shrink-0" />
              <span>
                {formatDateNL(series.startDate)} – {formatDateNL(series.endDate)}
              </span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <MapPin className="w-4 h-4 text-gray-400 shrink-0" />
              <span>{series.tennisClubName}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Euro className="w-4 h-4 text-gray-400 shrink-0" />
              <span>€{series.price.toFixed(2)} per reeks</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Users className="w-4 h-4 text-gray-400 shrink-0" />
              {series.maxRegistrations ? (
                <span>
                  {t("enrolledOfMax", {
                    enrolled: series.enrollmentCount,
                    max: series.maxRegistrations,
                  })}
                  {" — "}
                  <span
                    className={
                      series.enrollmentCount >= series.maxRegistrations
                        ? "text-red-600 font-medium"
                        : series.enrollmentCount / series.maxRegistrations >= 0.8
                          ? "text-amber-600 font-medium"
                          : "text-tennis-green font-medium"
                    }
                  >
                    {t("spotsLeft", {
                      count: Math.max(0, series.maxRegistrations - series.enrollmentCount),
                    })}
                  </span>
                </span>
              ) : (
                <span>{series.enrollmentCount} {t("enrolled").toLowerCase()}</span>
              )}
            </div>
          </div>
        </div>

        {/* Enrollment form card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          {submitted ? (
            <div className="flex flex-col items-center py-12 text-center px-6">
              <div className="w-14 h-14 rounded-full bg-green-100 flex items-center justify-center mb-4">
                <CheckCircle2 className="w-7 h-7 text-green-600" />
              </div>
              <h2 className="text-lg font-bold text-gray-900 mb-2">
                {t("enroll_success_title")}
              </h2>
              <p className="text-sm text-gray-500">{t("enroll_success_body")}</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} noValidate>
              {/* Form header */}
              <div className="px-6 py-5 border-b border-gray-100">
                <h2 className="text-lg font-semibold text-gray-900">
                  Inschrijving
                </h2>
                <p className="text-sm text-gray-500 mt-0.5">
                  Vul je gegevens in en kies je beschikbaarheid
                </p>
              </div>

              <div className="p-6 space-y-6">
                {submitError && (
                  <div className="px-4 py-3 rounded-lg bg-red-50 border border-red-100">
                    <p className="text-red-600 text-sm">{submitError}</p>
                  </div>
                )}

                {/* ── Personal info ── */}
                <div>
                  <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
                    Persoonlijke gegevens
                  </h3>
                  <div className="space-y-4">
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1.5">
                          {t("form_first_name")} *
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
                          {t("form_last_name")} *
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
                        {t("form_email")} *
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
                        {t("form_phone")}
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
                    {t("enrollment_type")}
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
                        <div>
                          <div className="font-medium text-gray-900 text-sm">
                            {t("type_solo")}
                          </div>
                          <div className="text-xs text-gray-500">
                            Ik schrijf mezelf in
                          </div>
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
                        <div>
                          <div className="font-medium text-gray-900 text-sm">
                            {t("type_group")}
                          </div>
                          <div className="text-xs text-gray-500">
                            Ik schrijf meerdere personen in
                          </div>
                        </div>
                      </div>
                    </label>
                  </div>

                  {/* Open to grouping (both solo and group) */}
                  {(enrollmentType === "solo" || enrollmentType === "group") && (
                    <div className="mt-3 p-3 bg-blue-50 rounded-lg border border-blue-100">
                      <label className="flex items-start gap-3 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={isOpenToGrouping}
                          onChange={(e) => setIsOpenToGrouping(e.target.checked)}
                          className="mt-0.5 w-4 h-4 rounded border-gray-300 text-tennis-green focus:ring-tennis-green"
                        />
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {t("open_to_grouping")}
                          </div>
                          <div className="text-xs text-gray-500 mt-0.5">
                            Het systeem kan je automatisch koppelen aan andere
                            leerlingen met dezelfde beschikbaarheid
                          </div>
                        </div>
                      </label>
                    </div>
                  )}

                  {/* Group: member fields */}
                  {enrollmentType === "group" && (
                    <div className="mt-4 space-y-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-700">
                          {t("group_members_title")} (max 3 extra)
                        </span>
                        {groupMembers.length < 3 && (
                          <button
                            type="button"
                            onClick={addGroupMember}
                            className="text-sm text-tennis-green hover:underline font-medium flex items-center gap-1"
                          >
                            <Plus size={14} />
                            {t("add_member")}
                          </button>
                        )}
                      </div>

                      {groupMembers.map((member, i) => (
                        <div
                          key={i}
                          className="border border-gray-200 rounded-lg p-3 space-y-3"
                        >
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2 text-xs text-gray-500">
                              <span className="w-5 h-5 rounded-full bg-tennis-green text-white flex items-center justify-center text-xs font-medium">
                                {i + 1}
                              </span>
                              Groepslid
                            </div>
                            <button
                              type="button"
                              onClick={() => removeGroupMember(i)}
                              className="text-xs text-gray-400 hover:text-red-500 flex items-center gap-1 transition-colors"
                            >
                              <X size={12} />
                              {t("remove_member")}
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
                                placeholder={t("member_name")}
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
                                placeholder={t("member_email")}
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

                {/* ── Availability grid ── */}
                {sortedSlots.length > 0 && (
                  <>
                    <hr className="border-gray-100" />
                    <div>
                      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                        {t("availability_title")}
                      </h3>
                      <p className="text-xs text-gray-500 mb-4">
                        {t("availability_desc")}
                      </p>

                      {/* Mobile legend — shown once above the grid */}
                      <div className="sm:hidden flex items-center gap-4 mb-3 text-xs text-gray-500">
                        <div className="flex items-center gap-1.5">
                          <div className="w-3 h-3 rounded-full bg-green-500" />
                          {t("pref_preferred")}
                        </div>
                        <div className="flex items-center gap-1.5">
                          <div className="w-3 h-3 rounded-full bg-blue-500" />
                          {t("pref_available")}
                        </div>
                        <div className="flex items-center gap-1.5">
                          <div className="w-3 h-3 rounded-full bg-gray-400" />
                          {t("pref_unavailable")}
                        </div>
                      </div>

                      <div className="border border-gray-200 rounded-lg overflow-hidden">
                        {/* Header — hidden on mobile */}
                        <div className="hidden sm:grid grid-cols-[1fr_100px_100px_100px] bg-gray-50 border-b border-gray-200">
                          <div className="px-4 py-2.5 text-xs font-semibold text-gray-500 uppercase">
                            Tijdslot
                          </div>
                          <div className="px-2 py-2.5 text-xs font-semibold text-green-700 uppercase text-center">
                            {t("pref_preferred")}
                          </div>
                          <div className="px-2 py-2.5 text-xs font-semibold text-blue-700 uppercase text-center">
                            {t("pref_available")}
                          </div>
                          <div className="px-2 py-2.5 text-xs font-semibold text-gray-500 uppercase text-center">
                            Niet besch.
                          </div>
                        </div>

                        {/* Rows grouped by day */}
                        {(() => {
                          const grouped: { day: number; slots: TimeSlotDto[] }[] = [];
                          for (const slot of sortedSlots) {
                            const last = grouped[grouped.length - 1];
                            if (last && last.day === slot.dayOfWeek) {
                              last.slots.push(slot);
                            } else {
                              grouped.push({ day: slot.dayOfWeek, slots: [slot] });
                            }
                          }

                          return grouped.map((group, gi) => (
                            <div
                              key={group.day}
                              className={gi < grouped.length - 1 ? "border-b border-gray-200" : ""}
                            >
                              {/* Day header */}
                              <div className="px-4 py-2 bg-gray-50/70 border-b border-gray-100">
                                <span className="text-xs font-semibold text-gray-700">
                                  {DAY_NAMES[group.day]}
                                </span>
                              </div>

                              {/* Slots for this day */}
                              {group.slots.map((slot, si) => (
                                <div
                                  key={slot.id}
                                  className={
                                    si < group.slots.length - 1 ? "border-b border-gray-100" : ""
                                  }
                                >
                                  {/* Desktop: table row */}
                                  <div className="hidden sm:grid grid-cols-[1fr_100px_100px_100px] hover:bg-gray-50/50">
                                    <div className="px-4 py-3">
                                      <div className="text-sm font-medium text-gray-900">
                                        {slot.startTime} — {slot.endTime}
                                      </div>
                                      <div className="text-xs text-gray-500">
                                        {slot.courtName}
                                      </div>
                                    </div>
                                    <div className="flex items-center justify-center">
                                      <PrefButton
                                        slotId={slot.id}
                                        value={PREF_PREFERRED}
                                        color={{ border: "#22c55e", bg: "#22c55e" }}
                                        icon="check"
                                      />
                                    </div>
                                    <div className="flex items-center justify-center">
                                      <PrefButton
                                        slotId={slot.id}
                                        value={PREF_AVAILABLE}
                                        color={{ border: "#3b82f6", bg: "#3b82f6" }}
                                        icon="check"
                                      />
                                    </div>
                                    <div className="flex items-center justify-center">
                                      <PrefButton
                                        slotId={slot.id}
                                        value={PREF_UNAVAILABLE}
                                        color={{ border: "#9ca3af", bg: "#9ca3af" }}
                                        icon="x"
                                      />
                                    </div>
                                  </div>

                                  {/* Mobile: compact layout — just colored circles */}
                                  <div className="sm:hidden px-4 py-3 flex items-center justify-between">
                                    <div>
                                      <div className="text-sm font-medium text-gray-900">
                                        {slot.startTime} — {slot.endTime}
                                      </div>
                                      <div className="text-xs text-gray-500">
                                        {slot.courtName}
                                      </div>
                                    </div>
                                    <div className="flex items-center gap-2.5 shrink-0">
                                      <PrefButton
                                        slotId={slot.id}
                                        value={PREF_PREFERRED}
                                        color={{ border: "#22c55e", bg: "#22c55e" }}
                                        icon="check"
                                      />
                                      <PrefButton
                                        slotId={slot.id}
                                        value={PREF_AVAILABLE}
                                        color={{ border: "#3b82f6", bg: "#3b82f6" }}
                                        icon="check"
                                      />
                                      <PrefButton
                                        slotId={slot.id}
                                        value={PREF_UNAVAILABLE}
                                        color={{ border: "#9ca3af", bg: "#9ca3af" }}
                                        icon="x"
                                      />
                                    </div>
                                  </div>
                                </div>
                              ))}
                            </div>
                          ));
                        })()}
                      </div>

                      {/* Legend — desktop only (mobile has inline labels) */}
                      <div className="hidden sm:flex items-center gap-4 mt-3 text-xs text-gray-500">
                        <div className="flex items-center gap-1.5">
                          <div className="w-3 h-3 rounded-full bg-green-500" />
                          {t("pref_preferred")}
                        </div>
                        <div className="flex items-center gap-1.5">
                          <div className="w-3 h-3 rounded-full bg-blue-500" />
                          {t("pref_available")}
                        </div>
                        <div className="flex items-center gap-1.5">
                          <div className="w-3 h-3 rounded-full bg-gray-400" />
                          {t("pref_unavailable")}
                        </div>
                      </div>
                    </div>
                  </>
                )}

                {/* ── Custom form fields ── */}
                {form && form.fields.length > 0 && (
                  <>
                    <hr className="border-gray-100" />
                    <div>
                      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
                        Extra vragen
                      </h3>
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
                  {t("form_submit")}
                </button>
                <p className="text-xs text-gray-400 text-center mt-3">
                  Je ontvangt een bevestiging per e-mail
                </p>
              </div>
            </form>
          )}

          {/* Share link for admin/trainer */}
          {isAdminOrTrainer && !submitted && (
            <div className="px-6 py-4 border-t border-gray-100">
              <Button
                variant="outline"
                className="w-full h-11 border-gray-200 cursor-pointer"
                onClick={handleCopyLink}
              >
                <Copy className="w-4 h-4 mr-2" />
                {copied ? t("enroll_link_copied") : t("share_link")}
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
