"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { CalendarDays, Clock, MapPin, User, Euro, CheckCircle2, Copy } from "lucide-react";

import {
  getPublicLessonSeries,
  getEnrollmentForm,
  submitEnrollment,
} from "@/lib/api/enrollments";
import type { PublicLessonSeriesDto, EnrollmentFormDto, FormFieldDto } from "@/lib/api/enrollments";
import { LESSON_LEVELS } from "@/lib/api/lessonSeries";
import { getAuthUser } from "@/lib/auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";
import Link from "next/link";

// FormFieldType enum values (mirrors backend)
const FIELD_TYPE_TEXT = 1;
const FIELD_TYPE_MULTIPLE_CHOICE = 2;
const FIELD_TYPE_YES_NO = 3;

function Spinner() {
  return (
    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
  );
}

function formatDate(dateStr: string): string {
  const [year, month, day] = dateStr.split("-");
  return `${day}/${month}/${year}`;
}

function inputClass(hasError: boolean) {
  return `w-full px-3 py-2 text-sm border rounded-lg outline-none transition-colors ${
    hasError
      ? "border-red-300 focus:border-red-400 bg-red-50"
      : "border-gray-200 focus:border-tennis-green"
  }`;
}

export default function EnrollPage() {
  const { seriesId } = useParams<{ seriesId: string }>();
  const t = useTranslations("enrollments");

  const [series, setSeries] = useState<PublicLessonSeriesDto | null>(null);
  const [form, setForm] = useState<EnrollmentFormDto | null>(null);
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
  const [fieldValues, setFieldValues] = useState<Record<string, string>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [baseErrors, setBaseErrors] = useState<{ firstName?: string; lastName?: string; email?: string }>({});

  const user = getAuthUser();
  const isAdminOrTrainer = user?.role === "Admin" || user?.role === "Trainer";

  useEffect(() => {
    async function loadData() {
      try {
        const [seriesData, formData] = await Promise.all([
          getPublicLessonSeries(seriesId),
          getEnrollmentForm(seriesId),
        ]);
        setSeries(seriesData);
        setForm(formData);
      } catch {
        setError("Lessenreeks niet gevonden.");
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [seriesId]);

  function setFieldValue(fieldId: string, value: string) {
    setFieldValues((prev) => ({ ...prev, [fieldId]: value }));
    if (fieldErrors[fieldId]) {
      setFieldErrors((prev) => { const next = { ...prev }; delete next[fieldId]; return next; });
    }
  }

  function validate(): boolean {
    const errors: typeof baseErrors = {};
    if (!firstName.trim()) errors.firstName = "Voornaam is verplicht";
    if (!lastName.trim()) errors.lastName = "Achternaam is verplicht";
    if (!email.trim()) errors.email = "E-mailadres is verplicht";
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) errors.email = "Ongeldig e-mailadres";
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

    return Object.keys(errors).length === 0 && Object.keys(fErrors).length === 0;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    if (!validate()) return;

    setSubmitting(true);
    try {
      const responses = Object.entries(fieldValues)
        .filter(([, v]) => v.trim())
        .map(([formFieldId, value]) => ({ formFieldId, value }));

      await submitEnrollment(seriesId, {
        studentName: `${firstName.trim()} ${lastName.trim()}`,
        studentEmail: email.trim(),
        responses,
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

  function renderCustomField(field: FormFieldDto) {
    const value = fieldValues[field.id] ?? "";
    const hasError = !!fieldErrors[field.id];

    if (field.type === FIELD_TYPE_MULTIPLE_CHOICE && field.options) {
      return (
        <div key={field.id} className="space-y-1.5">
          <label className="block text-sm font-medium text-gray-700">
            {field.label}{field.isRequired && <span className="text-red-500 ml-1">*</span>}
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
                  className="accent-tennis-green"
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
            {field.label}{field.isRequired && <span className="text-red-500 ml-1">*</span>}
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
                  className="accent-tennis-green"
                />
                <span className="text-sm text-gray-700">{opt}</span>
              </label>
            ))}
          </div>
          {hasError && <p className="text-xs text-red-500">{fieldErrors[field.id]}</p>}
        </div>
      );
    }

    // Default: text field (FIELD_TYPE_TEXT)
    return (
      <div key={field.id} className="space-y-1.5">
        <label className="block text-sm font-medium text-gray-700">
          {field.label}{field.isRequired && <span className="text-red-500 ml-1">*</span>}
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

  return (
    <div className="min-h-screen bg-[#FAFAF8]">
      {/* Header */}
      <header className="bg-tennis-green px-6 py-4 flex items-center gap-3">
        <div className="w-7 h-7 rounded-full bg-tennis-lime flex items-center justify-center">
          <TennisBallIcon className="w-4 h-4" strokeWidth={2} />
        </div>
        <span className="text-white text-lg font-bold tracking-tight">
          Coach<span className="text-tennis-lime">OS</span>
        </span>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-10">
        {/* Series header */}
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-8 mb-6">
          <div className="flex items-start justify-between gap-4 mb-6">
            <div>
              <Badge className="bg-tennis-lime/20 text-tennis-green border-0 mb-3 text-xs font-semibold">
                {LESSON_LEVELS[series.level] ?? "Niveau " + series.level}
              </Badge>
              <h1 className="text-2xl font-bold text-gray-900 mb-1">{series.name}</h1>
              {series.description && (
                <p className="text-gray-500 text-sm mt-2 leading-relaxed">{series.description}</p>
              )}
            </div>
            <div className="text-right flex-shrink-0">
              <p className="text-3xl font-bold text-tennis-green">€{series.price.toFixed(2)}</p>
              <p className="text-gray-400 text-xs mt-1">per reeks</p>
            </div>
          </div>

          {/* Details grid */}
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div className="flex items-center gap-2 text-gray-600">
              <User className="w-4 h-4 text-tennis-green flex-shrink-0" />
              <span>{series.trainerName}</span>
            </div>
            <div className="flex items-center gap-2 text-gray-600">
              <MapPin className="w-4 h-4 text-tennis-green flex-shrink-0" />
              <span>{series.tennisClubName}</span>
            </div>
            <div className="flex items-center gap-2 text-gray-600">
              <CalendarDays className="w-4 h-4 text-tennis-green flex-shrink-0" />
              <span>{formatDate(series.startDate)} – {formatDate(series.endDate)}</span>
            </div>
            <div className="flex items-center gap-2 text-gray-600">
              <Clock className="w-4 h-4 text-tennis-green flex-shrink-0" />
              <span>{series.durationMinutes} min</span>
            </div>
            <div className="flex items-center gap-2 text-gray-600">
              <Euro className="w-4 h-4 text-tennis-green flex-shrink-0" />
              <span>{series.enrollmentCount} {t("enrolled").toLowerCase()}</span>
            </div>
          </div>
        </div>

        {/* Lessons schedule */}
        {series.lessons.length > 0 && (
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Lesmomenten</h2>
            <div className="space-y-2">
              {series.lessons.map((lesson) => (
                <div
                  key={lesson.id}
                  className={`flex items-center justify-between py-2.5 px-3 rounded-lg text-sm ${
                    lesson.isCancelled ? "bg-gray-50 opacity-50" : "bg-[#FAFAF8]"
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <span className="font-medium text-gray-700">{formatDate(lesson.date)}</span>
                    {lesson.isCancelled && (
                      <Badge variant="secondary" className="text-xs">Geannuleerd</Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-3 text-gray-500">
                    <span>{lesson.startTime} – {lesson.endTime}</span>
                    {lesson.courtName && (
                      <span className="text-xs bg-gray-100 px-2 py-0.5 rounded">{lesson.courtName}</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Enrollment form / success */}
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
          {submitted ? (
            <div className="flex flex-col items-center py-6 text-center">
              <div className="w-14 h-14 rounded-full bg-green-100 flex items-center justify-center mb-4">
                <CheckCircle2 className="w-7 h-7 text-green-600" />
              </div>
              <h2 className="text-lg font-bold text-gray-900 mb-2">{t("enroll_success_title")}</h2>
              <p className="text-sm text-gray-500">{t("enroll_success_body")}</p>
            </div>
          ) : (
            <>
              <h2 className="text-base font-semibold text-gray-900 mb-5">Inschrijven</h2>

              {submitError && (
                <div className="mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
                  <p className="text-red-600 text-sm">{submitError}</p>
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-4">
                {/* Predefined: first + last name */}
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <label className="block text-sm font-medium text-gray-700">
                      {t("form_first_name")}<span className="text-red-500 ml-1">*</span>
                    </label>
                    <input
                      type="text"
                      value={firstName}
                      onChange={(e) => { setFirstName(e.target.value); setBaseErrors((p) => ({ ...p, firstName: undefined })); }}
                      className={inputClass(!!baseErrors.firstName)}
                    />
                    {baseErrors.firstName && <p className="text-xs text-red-500">{baseErrors.firstName}</p>}
                  </div>
                  <div className="space-y-1.5">
                    <label className="block text-sm font-medium text-gray-700">
                      {t("form_last_name")}<span className="text-red-500 ml-1">*</span>
                    </label>
                    <input
                      type="text"
                      value={lastName}
                      onChange={(e) => { setLastName(e.target.value); setBaseErrors((p) => ({ ...p, lastName: undefined })); }}
                      className={inputClass(!!baseErrors.lastName)}
                    />
                    {baseErrors.lastName && <p className="text-xs text-red-500">{baseErrors.lastName}</p>}
                  </div>
                </div>

                {/* Predefined: email */}
                <div className="space-y-1.5">
                  <label className="block text-sm font-medium text-gray-700">
                    {t("form_email")}<span className="text-red-500 ml-1">*</span>
                  </label>
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => { setEmail(e.target.value); setBaseErrors((p) => ({ ...p, email: undefined })); }}
                    className={inputClass(!!baseErrors.email)}
                  />
                  {baseErrors.email && <p className="text-xs text-red-500">{baseErrors.email}</p>}
                </div>

                {/* Custom fields */}
                {form && form.fields.length > 0 && (
                  <div className="space-y-4 pt-2 border-t border-gray-100">
                    {form.fields.map((field) => renderCustomField(field))}
                  </div>
                )}

                <Button
                  type="submit"
                  disabled={submitting}
                  className="w-full h-12 bg-tennis-green hover:bg-tennis-green/90 text-white font-semibold rounded-xl cursor-pointer mt-2"
                >
                  {submitting ? (
                    <span className="flex items-center gap-2"><Spinner />{t("form_submit")}...</span>
                  ) : (
                    t("form_submit")
                  )}
                </Button>
              </form>
            </>
          )}

          {/* Share link for admin/trainer */}
          {isAdminOrTrainer && !submitted && (
            <div className="mt-5 pt-5 border-t border-gray-100">
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
      </main>
    </div>
  );
}
