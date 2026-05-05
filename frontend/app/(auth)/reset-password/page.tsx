"use client";

import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";
import * as z from "zod";
import axios from "axios";

import { resetPassword } from "@/lib/api/auth";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const resetPasswordSchema = z
  .object({
    newPassword: z.string().min(8, "Wachtwoord moet minimaal 8 tekens bevatten"),
    confirmPassword: z.string().min(1),
  })
  .refine((d) => d.newPassword === d.confirmPassword, {
    message: "Wachtwoorden komen niet overeen",
    path: ["confirmPassword"],
  });

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

function Spinner() {
  return (
    <svg
      className="animate-spin h-4 w-4"
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle
        className="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        strokeWidth="4"
      />
      <path
        className="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
      />
    </svg>
  );
}

function ResetPasswordForm() {
  const t = useTranslations("auth");
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const email = searchParams.get("email") ?? "";

  const [isLoading, setIsLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  });

  async function onSubmit(data: ResetPasswordFormValues) {
    setIsLoading(true);
    setErrors([]);
    try {
      await resetPassword({ email, token, newPassword: data.newPassword });
      setSuccess(true);
      setTimeout(() => router.push("/login"), 3000);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const responseData = error.response.data;
        if (Array.isArray(responseData)) {
          setErrors(responseData);
        } else if (typeof responseData === "string") {
          setErrors([responseData]);
        } else {
          setErrors([t("resetPasswordError")]);
        }
      } else {
        setErrors([t("resetPasswordError")]);
      }
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex">
      {/* Left panel — ink branding */}
      <div className="hidden lg:flex lg:w-[44%] bg-ink relative overflow-hidden flex-col justify-between p-10">
        <CourtLines opacity={0.05} />

        {/* Subtle green glow */}
        <div
          className="absolute -right-[120px] -top-[80px] w-[320px] h-[320px] rounded-full blur-[10px]"
          style={{ background: "radial-gradient(closest-side, rgba(45,80,22,.55), transparent)" }}
        />

        {/* Logo + version */}
        <div className="relative z-10 flex items-center gap-2.5">
          <div className="w-[30px] h-[30px] rounded-md bg-tennis-lime grid place-items-center">
            <Mono className="text-ink font-extrabold text-[13px]">c/</Mono>
          </div>
          <span className="text-white font-bold text-base">CoachOS</span>
          <Mono className="ml-auto text-[10.5px] text-[#8a8377]">
            {t("heroVersion")}
          </Mono>
        </div>

        {/* Hero copy */}
        <div className="relative z-10">
          <SlashLabel className="text-tennis-lime mb-2.5">
            {t("heroRelease")}
          </SlashLabel>
          <h1 className="text-[34px] font-extrabold text-white leading-[1.05] tracking-tight mb-3">
            {t("heroHeadline1")}
            <br />
            {t("heroHeadline2")}{" "}
            <span className="text-tennis-lime">{t("heroHeadline3")}</span>
          </h1>
          <p className="text-[13px] text-[#a8a195] max-w-[300px] leading-relaxed">
            {t("tagline")}
          </p>
        </div>

        {/* Testimonial */}
        <div className="relative z-10 p-4 bg-white/[.04] rounded-[10px] border border-white/[.08]">
          <p className="text-[13px] leading-relaxed text-[#e9e4db] font-medium m-0">
            &ldquo;{t("testimonialQuote")}&rdquo;
          </p>
          <div className="flex items-center gap-2.5 mt-3">
            <div className="w-6 h-6 rounded-full bg-tennis-lime grid place-items-center">
              <Mono className="text-ink text-[10px] font-bold">SD</Mono>
            </div>
            <div>
              <p className="text-[11px] text-white font-semibold m-0">
                {t("testimonialName")}
              </p>
              <Mono className="text-[10.5px] text-[#8a8377]">
                {t("testimonialClub")}
              </Mono>
            </div>
          </div>
        </div>
      </div>

      {/* Right panel — form */}
      <div className="w-full lg:w-[56%] flex items-center justify-center p-8 bg-paper">
        <div className="w-full max-w-[340px]">
          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-10 lg:hidden">
            <div className="w-7 h-7 rounded-md bg-tennis-green grid place-items-center">
              <Mono className="text-tennis-lime font-extrabold text-[12px]">c/</Mono>
            </div>
            <span className="text-ink text-xl font-bold">CoachOS</span>
          </div>

          {/* Heading */}
          <div className="mb-5">
            <SlashLabel>/nieuw-wachtwoord</SlashLabel>
            <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
              {t("resetPasswordTitle")}
            </h2>
            <p className="text-[12.5px] text-ink-3">
              {t("resetPasswordSub")}
            </p>
          </div>

          {/* Success state */}
          {success ? (
            <div className="space-y-4">
              <div className="px-4 py-3 rounded-lg bg-green-50 border border-green-100">
                <p className="text-green-700 text-sm">{t("resetPasswordSuccess")}</p>
              </div>
              <Link
                href="/login"
                className="block text-center text-[12.5px] text-tennis-green font-semibold hover:underline"
              >
                {t("loginButton")} →
              </Link>
            </div>
          ) : (
            <>
              {/* Error banner */}
              {errors.length > 0 && (
                <div className="mb-5 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
                  {errors.map((err, i) => (
                    <p key={i} className="text-red-600 text-sm">
                      {err}
                    </p>
                  ))}
                </div>
              )}

              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3.5">
                  <FormField
                    control={form.control}
                    name="newPassword"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]">
                          {t("newPassword")}
                        </FormLabel>
                        <FormControl>
                          <Input
                            {...field}
                            type="password"
                            autoComplete="new-password"
                            className="h-10 bg-white border-rule rounded-lg text-ink text-[12.5px] focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green"
                          />
                        </FormControl>
                        <FormMessage className="text-xs" />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="confirmPassword"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]">
                          {t("confirmPassword")}
                        </FormLabel>
                        <FormControl>
                          <Input
                            {...field}
                            type="password"
                            autoComplete="new-password"
                            className="h-10 bg-white border-rule rounded-lg text-ink text-[12.5px] focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green"
                          />
                        </FormControl>
                        <FormMessage className="text-xs" />
                      </FormItem>
                    )}
                  />

                  <Button
                    type="submit"
                    disabled={isLoading}
                    className="w-full h-[42px] bg-ink hover:bg-ink/90 text-white font-bold text-[13px] rounded-lg cursor-pointer"
                  >
                    {isLoading ? (
                      <span className="flex items-center gap-2">
                        <Spinner />
                        {t("loading")}
                      </span>
                    ) : (
                      t("resetPasswordButton")
                    )}
                  </Button>
                </form>
              </Form>

              <p className="mt-5 text-center text-[11.5px] text-ink-3">
                <Link
                  href="/login"
                  className="text-tennis-green font-semibold hover:underline"
                >
                  ← {t("backToLogin")}
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

export default function ResetPasswordPage() {
  return (
    <Suspense>
      <ResetPasswordForm />
    </Suspense>
  );
}
