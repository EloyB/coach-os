"use client";

import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";
import * as z from "zod";
import axios from "axios";

import { register } from "@/lib/api/auth";
import { setToken, setAuthUser } from "@/lib/auth";
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

const registerSchema = z
  .object({
    organizationName: z.string().min(1, "Naam organisatie is verplicht").max(200),
    firstName: z.string().min(1, "Voornaam is verplicht").max(100),
    lastName: z.string().min(1, "Achternaam is verplicht").max(100),
    email: z.email(),
    password: z.string().min(8, "Wachtwoord moet minimaal 8 tekens bevatten"),
    confirmPassword: z.string().min(1),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Wachtwoorden komen niet overeen",
    path: ["confirmPassword"],
  });

type RegisterFormValues = z.infer<typeof registerSchema>;

function PasswordChip({ label, met }: { label: string; met: boolean }) {
  return (
    <span
      className={`text-[10px] font-mono px-2 py-0.5 rounded-full inline-flex items-center gap-1 ${
        met
          ? "bg-green-600/10 text-green-700"
          : "bg-canvas text-ink-3"
      }`}
    >
      {met ? "✓" : "○"} {label}
    </span>
  );
}

function RegisterForm() {
  const t = useTranslations("auth");
  const router = useRouter();
  const searchParams = useSearchParams();
  // Optional plan/interval hints from the marketing pricing page (e.g.
  // /register?plan=pro&interval=yearly). Non-binding in Phase 1 — the register
  // API doesn't accept them yet, so we just carry them along to the post-signup
  // destination for a future plan-selection step instead of dropping them.
  const plan = searchParams.get("plan");
  const interval = searchParams.get("interval");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      organizationName: "",
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
  });

  const watchPassword = form.watch("password");
  const pwdRules = [
    { label: t("pwdRuleLength"), met: watchPassword.length >= 8 },
    { label: t("pwdRuleUpper"), met: /[A-Z]/.test(watchPassword) },
    { label: t("pwdRuleDigit"), met: /\d/.test(watchPassword) },
  ];

  async function onSubmit(data: RegisterFormValues) {
    setIsLoading(true);
    setErrors([]);
    try {
      const response = await register({
        organizationName: data.organizationName,
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
      });
      setToken(response.token);
      setAuthUser({
        userId: response.userId,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        organizationId: response.organizationId,
        role: response.role,
        memberships: response.memberships,
      });
      if (plan) {
        const destination = new URLSearchParams({ plan });
        if (interval) destination.set("interval", interval);
        router.push(`/dashboard?${destination.toString()}`);
      } else {
        router.push("/dashboard");
      }
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        if (Array.isArray(data)) {
          setErrors(data);
        } else if (typeof data === "string") {
          setErrors([data]);
        } else {
          setErrors([t("registerError")]);
        }
      } else {
        setErrors([t("registerError")]);
      }
    } finally {
      setIsLoading(false);
    }
  }

  const inputCls =
    "h-10 bg-white border-rule rounded-lg text-ink text-[12.5px] placeholder:text-[#c5c0b6] focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green";

  return (
    <div className="min-h-screen flex">
      {/* Left panel — ink */}
      <div className="hidden lg:flex lg:w-[42%] bg-ink relative overflow-hidden flex-col justify-between p-9 text-white">
        <CourtLines opacity={0.05} />
        <div className="relative z-10">
          <SlashLabel className="text-tennis-lime">
            /sign-up · v2026.04
          </SlashLabel>
        </div>
        <div className="relative z-10">
          <h1 className="text-[40px] font-extrabold leading-[1.02] tracking-tight m-0">
            {t("registerHeroLine1")}
            <br />
            {t("registerHeroLine2")}
            <br />
            <span className="text-tennis-lime">{t("registerHeroLine3")}</span>
          </h1>
          <p className="text-[13px] text-[#a8a195] max-w-[300px] leading-relaxed mt-[18px]">
            {t("registerTagline")}
          </p>
        </div>
        <div className="relative z-10">
          <Mono className="text-[10.5px] text-[#8a8377] flex flex-col gap-1">
            <span>{t("registerClubs")}</span>
          </Mono>
        </div>
      </div>

      {/* Right panel — form */}
      <div className="w-full lg:flex-1 flex items-center justify-center p-7 bg-paper overflow-y-auto">
        <div className="w-full max-w-[400px]">
          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-7 lg:hidden">
            <div className="w-7 h-7 rounded-md bg-tennis-green grid place-items-center">
              <Mono className="text-tennis-lime font-extrabold text-[12px]">c/</Mono>
            </div>
            <span className="text-ink text-xl font-bold">CoachOS</span>
          </div>

          <SlashLabel>{t("registerStep")}</SlashLabel>
          <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-5">
            {t("registerHeadline")}
          </h2>

          {errors.length > 0 && (
            <div className="mb-5 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              {errors.map((err, i) => (
                <p key={i} className="text-red-600 text-sm">{err}</p>
              ))}
            </div>
          )}

          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
              <FormField
                control={form.control}
                name="organizationName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-[11px] text-ink-2 font-medium">
                      {t("organizationName")}
                    </FormLabel>
                    <FormControl>
                      <Input {...field} placeholder="bv. TC Brederode" className={inputCls} />
                    </FormControl>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-2 gap-2.5">
                <FormField
                  control={form.control}
                  name="firstName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="text-[11px] text-ink-2 font-medium">
                        {t("firstName")}
                      </FormLabel>
                      <FormControl>
                        <Input {...field} placeholder="Jan" className={inputCls} />
                      </FormControl>
                      <FormMessage className="text-xs" />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="lastName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="text-[11px] text-ink-2 font-medium">
                        {t("lastName")}
                      </FormLabel>
                      <FormControl>
                        <Input {...field} placeholder="Janssen" className={inputCls} />
                      </FormControl>
                      <FormMessage className="text-xs" />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-[11px] text-ink-2 font-medium">
                      {t("email")}
                    </FormLabel>
                    <FormControl>
                      <Input {...field} type="email" placeholder={t("emailPlaceholder")} className={inputCls} />
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
                    <FormLabel className="text-[11px] text-ink-2 font-medium">
                      {t("password")}
                    </FormLabel>
                    <FormControl>
                      <Input {...field} type="password" autoComplete="new-password" className={inputCls} />
                    </FormControl>
                    <div className="flex gap-1.5 mt-1.5">
                      {pwdRules.map((r) => (
                        <PasswordChip key={r.label} label={r.label} met={r.met} />
                      ))}
                    </div>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-[11px] text-ink-2 font-medium">
                      {t("confirmPassword")}
                    </FormLabel>
                    <FormControl>
                      <Input {...field} type="password" autoComplete="new-password" className={inputCls} />
                    </FormControl>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              <Button
                type="submit"
                disabled={isLoading}
                className="w-full h-11 bg-ink hover:bg-ink/90 text-white font-semibold text-[13px] rounded-lg mt-2.5 cursor-pointer"
              >
                {isLoading ? t("loading") : t("registerCta")}
              </Button>
            </form>
          </Form>

          <p className="mt-5 text-center text-[11.5px] text-ink-3">
            {t("hasAccount")}{" "}
            <Link href="/login" className="text-tennis-green font-semibold">
              {t("hasAccountCta")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

export default function RegisterPage() {
  return (
    <Suspense>
      <RegisterForm />
    </Suspense>
  );
}
