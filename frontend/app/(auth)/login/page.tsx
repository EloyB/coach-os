"use client";

import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";
import * as z from "zod";
import axios from "axios";

import { login } from "@/lib/api/auth";
import { setToken, setAuthUser, setSuperAdminCookie } from "@/lib/auth";
import { CourtLines } from "@/components/ui/court-lines";
import { LogoMark } from "@/components/ui/logo-mark";
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

const loginSchema = z.object({
  email: z.email(),
  password: z.string().min(1),
});

type LoginFormValues = z.infer<typeof loginSchema>;

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

function LoginForm() {
  const t = useTranslations("auth");
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectTo = searchParams.get("redirect");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  async function onSubmit(data: LoginFormValues) {
    setIsLoading(true);
    setErrors([]);
    try {
      const response = await login(data);
      setToken(response.token);
      setSuperAdminCookie(response.isSuperAdmin === true);
      setAuthUser({
        userId: response.userId,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        organizationId: response.organizationId,
        role: response.role,
        memberships: response.memberships,
        isSuperAdmin: response.isSuperAdmin,
      });
      const landing = response.isSuperAdmin ? "/super-admin/dashboard" : "/dashboard";
      router.push(redirectTo ?? landing);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        if (Array.isArray(data)) {
          setErrors(data);
        } else if (typeof data === "string") {
          setErrors([data]);
        } else {
          setErrors([t("loginError")]);
        }
      } else {
        setErrors([t("loginError")]);
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
          <LogoMark className="h-[30px] w-[30px]" markPx={22} />
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
            <LogoMark variant="green" />
            <span className="text-ink text-xl font-bold">CoachOS</span>
          </div>

          {/* Heading */}
          <div className="mb-5">
            <SlashLabel>/login</SlashLabel>
            <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
              {t("welcomeBack")}
            </h2>
            <p className="text-[12.5px] text-ink-3">
              {t("welcomeBackSub")}
            </p>
          </div>

          {/* Role segment */}
          <div className="flex border border-rule rounded-lg p-[3px] bg-canvas mb-5">
            <div className="flex-1 py-[7px] text-center bg-white rounded-md text-[11.5px] font-semibold text-ink shadow-[0_1px_2px_rgba(0,0,0,.04)]">
              {t("roleCoach")}
            </div>
            <Link
              href="/student-login"
              className="flex-1 py-[7px] text-center text-[11.5px] text-ink-3"
            >
              {t("roleStudent")}
            </Link>
          </div>

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
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]">
                      {t("email")}
                    </FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="email"
                        autoComplete="email"
                        placeholder={t("emailPlaceholder")}
                        className="h-10 bg-white border-rule rounded-lg text-ink text-[12.5px] placeholder:text-[#c5c0b6] focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green"
                      />
                    </FormControl>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <div className="flex items-center justify-between">
                      <FormLabel className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]">
                        {t("password")}
                      </FormLabel>
                      <Link
                        href="/forgot-password"
                        className="text-[11px] text-tennis-green font-semibold hover:underline"
                      >
                        {t("forgotPassword")}
                      </Link>
                    </div>
                    <FormControl>
                      <Input
                        {...field}
                        type="password"
                        autoComplete="current-password"
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
                  <>
                    {t("loginButton")} →
                  </>
                )}
              </Button>
            </form>
          </Form>

          {/* Divider */}
          <div className="flex items-center gap-2.5 my-4">
            <div className="flex-1 h-px bg-rule" />
            <Mono className="text-[10.5px] text-ink-3">{t("or")}</Mono>
            <div className="flex-1 h-px bg-rule" />
          </div>

          {/* Magic link */}
          <Button
            variant="outline"
            className="w-full h-10 bg-white text-ink text-xs font-semibold border-rule rounded-lg"
          >
            {t("magicLink")}
          </Button>

          <p className="mt-5 text-center text-[11.5px] text-ink-3">
            {t("noAccount")}{" "}
            <Link
              href="/register"
              className="text-ink font-bold border-b border-tennis-lime"
            >
              {t("noAccountCta")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
