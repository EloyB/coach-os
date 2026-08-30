"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import {
  CalendarDays,
  MapPin,
  User,
  Users,
  Euro,
  Cake,
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
import { type LessonSeriePriceDto } from "@/lib/api/lessonSeriePrices";
import { getAuthUser } from "@/lib/auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { LogoMark } from "@/components/ui/logo-mark";
import { Spinner } from "@/components/ui/spinner";
import { formatDateNL } from "@/lib/date-utils";
import Link from "next/link";

// ─── Constants ───────────────────────────────────────────────────────────────

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

type GroupMember = {
  name: string;
  email: string;
  dateOfBirth: string;
  hasOwnEmail: boolean;
};

// ─── Helpers ─────────────────────────────────────────────────────────────────

/**
 * Valideert een geboortedatum (yyyy-MM-dd uit een date-input). Spiegelt de
 * backendregels in DateOfBirthRules, zodat de gebruiker de fout hier al ziet.
 */
function validateBirthDate(value: string): string | undefined {
  if (!value.trim()) return "Geboortedatum is verplicht";

  const parsed = new Date(value + "T00:00:00");
  if (Number.isNaN(parsed.getTime())) return "Ongeldige geboortedatum";

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  if (parsed > today) return "Geboortedatum kan niet in de toekomst liggen";

  const maxAge = 120;
  const oldest = new Date(today);
  oldest.setFullYear(oldest.getFullYear() - maxAge);
  if (parsed < oldest) return "Controleer de geboortedatum";

  return undefined;
}

/** Leeftijd in hele jaren op een peildatum (yyyy-MM-dd strings). */
function ageOn(dob: string, onDate: string): number | null {
  if (!dob || !onDate) return null;
  const b = new Date(dob + "T00:00:00");
  const d = new Date(onDate + "T00:00:00");
  if (Number.isNaN(b.getTime()) || Number.isNaN(d.getTime())) return null;
  let age = d.getFullYear() - b.getFullYear();
  const m = d.getMonth() - b.getMonth();
  if (m < 0 || (m === 0 && d.getDate() < b.getDate())) age--;
  return age;
}

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
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [fieldValues, setFieldValues] = useState<Record<string, string>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [baseErrors, setBaseErrors] = useState<{
    firstName?: string;
    lastName?: string;
    email?: string;
    dateOfBirth?: string;
  }>({});

  // Availability preferences
  const [preferences, setPreferences] = useState<Record<string, number>>({});

  // Enrollment type. De reeks bepaalt welke wijzen toegelaten zijn; "solo" is
  // hier enkel een placeholder tot de reeks geladen is (zie loadData hieronder,
  // die het type corrigeert naar de eerst-toegelaten wijze).
  const [enrollmentType, setEnrollmentType] = useState<"solo" | "group">(
    "solo"
  );
  const [isOpenToGrouping, setIsOpenToGrouping] = useState(false);
  const [groupMembers, setGroupMembers] = useState<GroupMember[]>([]);
  const [selectedPriceOptionId, setSelectedPriceOptionId] = useState<string>("");
  const [memberErrors, setMemberErrors] = useState<
    Record<number, { name?: string; email?: string; dateOfBirth?: string }>
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
        // Default naar de eerst-toegelaten inschrijfwijze zodra de reeks bekend is.
        setEnrollmentType(
          seriesData.allowSoloEnrollment ? "solo" : "group"
        );
        // Eén optie? Automatisch geselecteerd. Meerdere? De speler kiest.
        setSelectedPriceOptionId(
          seriesData.priceOptions.length === 1 ? seriesData.priceOptions[0].id : ""
        );
      } catch {
        setError("Lessenreeks niet gevonden.");
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [seriesId]);

  // Reset alle formuliervelden zodat iemand meteen een nieuwe inschrijving kan
  // doen zonder de pagina te verversen. Ververst ook de reeks zodat het aantal
  // ingeschrevenen/vrije plekken klopt na de vorige inschrijving.
  async function resetForm() {
    setSubmitted(false);
    setSubmitError(null);
    setFirstName("");
    setLastName("");
    setEmail("");
    setPhone("");
    setDateOfBirth("");
    setFieldValues({});
    setFieldErrors({});
    setBaseErrors({});
    setPreferences({});
    setIsOpenToGrouping(false);
    setGroupMembers([]);
    setMemberErrors({});
    setEnrollmentType(series?.allowSoloEnrollment ? "solo" : "group");
    setSelectedPriceOptionId(
      series && series.priceOptions.length === 1 ? series.priceOptions[0].id : ""
    );
    window.scrollTo({ top: 0, behavior: "smooth" });
    try {
      const fresh = await getPublicLessonSeries(seriesId);
      setSeries(fresh);
    } catch {
      // Niet fataal — het formulier is al gereset.
    }
  }

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
    setGroupMembers((prev) => [
      ...prev,
      { name: "", email: "", dateOfBirth: "", hasOwnEmail: false },
    ]);
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
    field: "name" | "email" | "dateOfBirth",
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

  function toggleMemberOwnEmail(index: number, hasOwnEmail: boolean) {
    setGroupMembers((prev) =>
      prev.map((m, i) =>
        i === index ? { ...m, hasOwnEmail, email: hasOwnEmail ? m.email : "" } : m
      )
    );
  }

  function priceOptions(): LessonSeriePriceDto[] {
    return series?.priceOptions ?? [];
  }

  function formatPriceOption(option: LessonSeriePriceDto): string {
    return `€${option.totalPrice.toFixed(2)} per deelnemer`;
  }

  // ─── Validation ─────────────────────────────────────────────────────────

  function validate(): boolean {
    const errors: typeof baseErrors = {};
    if (!firstName.trim()) errors.firstName = "Voornaam is verplicht";
    if (!lastName.trim()) errors.lastName = "Achternaam is verplicht";
    if (!email.trim()) errors.email = "E-mailadres is verplicht";
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email))
      errors.email = "Ongeldig e-mailadres";
    const dobError = validateBirthDate(dateOfBirth);
    if (dobError) errors.dateOfBirth = dobError;
    if (series) {
      const leaderAge = ageOn(dateOfBirth, series.startDate);
      if (leaderAge !== null && (leaderAge < series.minAge || leaderAge > series.maxAge)) {
        errors.dateOfBirth = `Leeftijd moet tussen ${series.minAge} en ${series.maxAge} jaar zijn`;
      }
    }
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

    const mErrors: Record<
      number,
      { name?: string; email?: string; dateOfBirth?: string }
    > = {};
    if (enrollmentType === "group") {
      groupMembers.forEach((m, i) => {
        const e: { name?: string; email?: string; dateOfBirth?: string } = {};
        if (!m.name.trim()) e.name = "Naam is verplicht";
        if (m.hasOwnEmail) {
          if (!m.email.trim()) e.email = "E-mailadres is verplicht";
          else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(m.email))
            e.email = "Ongeldig e-mailadres";
        }
        const memberDobError = validateBirthDate(m.dateOfBirth);
        if (memberDobError) e.dateOfBirth = memberDobError;
        if (series) {
          const memberAge = ageOn(m.dateOfBirth, series.startDate);
          if (memberAge !== null && (memberAge < series.minAge || memberAge > series.maxAge)) {
            e.dateOfBirth = `Leeftijd moet tussen ${series.minAge} en ${series.maxAge} jaar zijn`;
          }
        }
        if (e.name || e.email || e.dateOfBirth) mErrors[i] = e;
      });

      // Dubbele deelnemer (naam + geboortedatum) — spiegelt de backend zonder
      // server-lookup, dus zonder te lekken wie al ingeschreven is.
      const people = [
        `${firstName.trim().toLowerCase()} ${lastName.trim().toLowerCase()}|${dateOfBirth}`,
        ...groupMembers.map(
          (m) => `${m.name.trim().toLowerCase()}|${m.dateOfBirth}`
        ),
      ];
      groupMembers.forEach((m, i) => {
        if (!m.name.trim() || !m.dateOfBirth) return;
        const key = `${m.name.trim().toLowerCase()}|${m.dateOfBirth}`;
        if (people.filter((p) => p === key).length > 1) {
          mErrors[i] = { ...mErrors[i], name: t("duplicate_participant") };
        }
      });
    }
    setMemberErrors(mErrors);

    const requiresPriceChoice = priceOptions().length > 0;
    if (requiresPriceChoice && !selectedPriceOptionId) {
      setSubmitError("Kies een prijsoptie om verder te gaan.");
      return false;
    }

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
        dateOfBirth: dateOfBirth,
        responses,
        timeSlotPreferences:
          timeSlotPreferences.length > 0 ? timeSlotPreferences : undefined,
        enrollmentType,
        isOpenToGrouping,
        selectedPriceOptionId: selectedPriceOptionId || undefined,
        groupMembers:
          enrollmentType === "group" && groupMembers.length > 0
            ? groupMembers.map((m) => ({
                studentName: m.name.trim(),
                studentEmail: m.hasOwnEmail ? m.email.trim() : null,
                dateOfBirth: m.dateOfBirth,
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

  // Niveau is optioneel — geen badge tonen als het niet ingevuld is
  const levelLabel =
    series.level != null ? LESSON_LEVELS[series.level] : undefined;

  // ─── Render ─────────────────────────────────────────────────────────────

  return (
    <div className="min-h-screen bg-[#FAFAF8]">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Logo */}
        <div className="flex items-center gap-2 mb-8">
          <LogoMark className="h-8 w-8" markPx={22} />
          <span className="font-semibold text-lg text-tennis-green">CoachOS</span>
        </div>

        {/* Series info card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          {levelLabel && (
            <Badge className="bg-tennis-lime/20 text-tennis-green border-0 mb-2 text-xs font-semibold">
              {levelLabel}
            </Badge>
          )}
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
              <span>
                {series.priceOptions.length > 0
                  ? `${series.priceOptions.length} prijsoptie${series.priceOptions.length === 1 ? "" : "s"}`
                  : `€${series.price.toFixed(2)} per deelnemer`}
              </span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Cake className="w-4 h-4 text-gray-400 shrink-0" />
              <span>Leeftijd: {series.minAge}–{series.maxAge} jaar</span>
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

        {!submitted && series.priceOptions.length > 0 && (
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 mb-6">
            <div className="flex items-center gap-2 mb-3">
              <Euro className="w-4 h-4 text-tennis-green" />
              <h2 className="text-sm font-semibold text-gray-900">Prijsopties</h2>
            </div>
            {priceOptions().length > 0 && (
              <div>
                <p className="text-xs text-gray-500 mb-2">Kies de prijsoptie die voor jou van toepassing is.</p>
                <div className="space-y-2">
                  {priceOptions().map((option) => (
                    <label key={option.id} className="block cursor-pointer rounded-lg border border-gray-200 p-3 hover:border-tennis-green/40">
                      <div className="flex items-start gap-3">
                        <input
                          type="radio"
                          name="selectedPriceOption"
                          value={option.id}
                          checked={selectedPriceOptionId === option.id}
                          onChange={() => setSelectedPriceOptionId(option.id)}
                          className="mt-1 w-4 h-4 text-tennis-green focus:ring-tennis-green"
                        />
                        <div className="flex-1">
                          <div className="flex items-start justify-between gap-3">
                            <span className="text-sm font-medium text-gray-800">{option.label}</span>
                            <span className="text-sm font-semibold text-tennis-green whitespace-nowrap">{formatPriceOption(option)}</span>
                          </div>
                          {option.description && <p className="text-xs text-gray-500 mt-1">{option.description}</p>}
                        </div>
                      </div>
                    </label>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

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
              <button
                type="button"
                onClick={resetForm}
                className="mt-6 text-sm font-medium text-tennis-green hover:underline"
              >
                {t("enroll_again")}
              </button>
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
                    <div>
                      <label
                        htmlFor="dateOfBirth"
                        className="block text-sm font-medium text-gray-700 mb-1.5"
                      >
                        {t("form_date_of_birth")} *
                      </label>
                      <input
                        id="dateOfBirth"
                        type="date"
                        value={dateOfBirth}
                        onChange={(e) => {
                          setDateOfBirth(e.target.value);
                          if (baseErrors.dateOfBirth) {
                            setBaseErrors((prev) => ({
                              ...prev,
                              dateOfBirth: undefined,
                            }));
                          }
                        }}
                        max={new Date().toISOString().slice(0, 10)}
                        className={inputClass(!!baseErrors.dateOfBirth)}
                      />
                      {baseErrors.dateOfBirth ? (
                        <p className="text-xs text-red-500 mt-1">
                          {baseErrors.dateOfBirth}
                        </p>
                      ) : (
                        <p className="text-xs text-gray-400 mt-1">
                          {t("form_date_of_birth_hint")}
                        </p>
                      )}
                    </div>
                  </div>
                </div>

                <hr className="border-gray-100" />

                {/* ── Enrollment type ── */}
                {/* Enkel tonen als er echt een keuze is: bij één toegelaten wijze staat
                    enrollmentType al vast (zie loadData) en is er niets te kiezen. */}
                <div>
                  {series.allowSoloEnrollment && series.allowGroupEnrollment && (
                    <>
                      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
                        {t("enrollment_type")}
                      </h3>
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        {series.allowSoloEnrollment && (
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
                        )}

                        {series.allowGroupEnrollment && (
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
                        )}
                      </div>
                    </>
                  )}

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
                            {t("open_to_grouping_desc")}
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

                      <p className="text-xs text-gray-500">
                        {t("group_contact_explainer")}
                      </p>

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
                            <div className="sm:col-span-2">
                              <label className="flex items-center gap-2 text-xs text-gray-600">
                                <input
                                  type="checkbox"
                                  checked={member.hasOwnEmail}
                                  onChange={(e) =>
                                    toggleMemberOwnEmail(i, e.target.checked)
                                  }
                                  className="rounded border-gray-300 text-tennis-green focus:ring-tennis-green/20"
                                />
                                {t("member_has_own_email")}
                              </label>

                              {member.hasOwnEmail ? (
                                <div className="mt-2">
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
                              ) : (
                                <p className="text-xs text-gray-400 mt-1">
                                  {t("member_contact_via_leader", {
                                    email: email.trim() || "…",
                                  })}
                                </p>
                              )}
                            </div>
                            <div className="sm:col-span-2">
                              <label className="block text-xs text-gray-500 mb-1">
                                {t("form_member_date_of_birth")} *
                              </label>
                              <input
                                type="date"
                                aria-label={`${t("form_member_date_of_birth")} ${i + 1}`}
                                value={member.dateOfBirth}
                                onChange={(e) =>
                                  updateGroupMember(i, "dateOfBirth", e.target.value)
                                }
                                max={new Date().toISOString().slice(0, 10)}
                                className={inputClass(!!memberErrors[i]?.dateOfBirth)}
                              />
                              {memberErrors[i]?.dateOfBirth && (
                                <p className="text-xs text-red-500 mt-1">
                                  {memberErrors[i].dateOfBirth}
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
