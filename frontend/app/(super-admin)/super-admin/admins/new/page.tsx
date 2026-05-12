"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import * as z from "zod";
import axios from "axios";

import { createAdmin } from "@/lib/api/super-admin";
import { inputClass } from "@/lib/styles";
import { FieldError } from "@/components/forms/field-error";

const schema = z.object({
  organizationName: z.string().min(1).max(200),
  firstName: z.string().min(1).max(100),
  lastName: z.string().min(1).max(100),
  email: z.email(),
  isEarlyBird: z.boolean(),
});
type FormValues = z.infer<typeof schema>;

export default function NewAdminPage() {
  const t = useTranslations("superAdmin");
  const router = useRouter();
  const [apiErrors, setApiErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      organizationName: "",
      firstName: "",
      lastName: "",
      email: "",
      isEarlyBird: false,
    },
  });

  async function onSubmit(values: FormValues) {
    setSubmitting(true);
    setApiErrors([]);
    try {
      await createAdmin(values);
      router.push("/super-admin/admins");
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        if (Array.isArray(data)) setApiErrors(data);
        else if (typeof data === "string") setApiErrors([data]);
        else setApiErrors([t("createError")]);
      } else {
        setApiErrors([t("createError")]);
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="max-w-xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">{t("createTitle")}</h1>
        <p className="text-sm text-ink-3 mt-1">{t("createSubtitle")}</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {apiErrors.length > 0 && (
          <div className="bg-red-50 border border-red-100 rounded-lg px-4 py-3 text-sm text-red-600">
            {apiErrors.map((e, i) => (
              <p key={i}>{e}</p>
            ))}
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            {t("fieldOrganization")} <span className="text-red-400">*</span>
          </label>
          <input
            {...register("organizationName")}
            type="text"
            placeholder="TC Brederode"
            className={inputClass}
          />
          <FieldError message={errors.organizationName?.message} />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              {t("fieldFirstName")} <span className="text-red-400">*</span>
            </label>
            <input
              {...register("firstName")}
              type="text"
              placeholder="Jan"
              className={inputClass}
            />
            <FieldError message={errors.firstName?.message} />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              {t("fieldLastName")} <span className="text-red-400">*</span>
            </label>
            <input
              {...register("lastName")}
              type="text"
              placeholder="Janssen"
              className={inputClass}
            />
            <FieldError message={errors.lastName?.message} />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            {t("fieldEmail")} <span className="text-red-400">*</span>
          </label>
          <input
            {...register("email")}
            type="email"
            placeholder="jan@tennisclub.be"
            className={inputClass}
          />
          <FieldError message={errors.email?.message} />
        </div>

        <label className="flex items-center gap-2.5 pt-1 cursor-pointer select-none">
          <input
            {...register("isEarlyBird")}
            type="checkbox"
            className="h-4 w-4 rounded border-gray-300 text-tennis-green focus:ring-tennis-green/30"
          />
          <span className="text-sm text-gray-700">{t("fieldEarlyBird")}</span>
        </label>

        <div className="flex items-center gap-3 pt-2">
          <button
            type="button"
            onClick={() => router.back()}
            className="flex-1 px-4 py-2.5 border border-gray-200 text-sm font-medium text-gray-600 rounded-lg hover:bg-gray-50 transition-colors"
          >
            {t("cancel")}
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 px-4 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {submitting ? t("submitting") : t("createSubmit")}
          </button>
        </div>
      </form>
    </div>
  );
}
