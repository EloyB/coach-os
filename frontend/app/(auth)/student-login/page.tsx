"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import Link from "next/link";
import * as z from "zod";

import { requestMagicLink } from "@/lib/api/auth";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const schema = z.object({
  email: z.email(),
});

type FormValues = z.infer<typeof schema>;

export default function StudentLoginPage() {
  const t = useTranslations("studentAuth");
  const tAuth = useTranslations("auth");
  const [isLoading, setIsLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "" },
  });

  async function onSubmit(data: FormValues) {
    setIsLoading(true);
    try {
      await requestMagicLink(data.email);
      setSent(true);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex">
      {/* Left panel — ink branding (same layout as login) */}
      <div className="hidden lg:flex lg:w-[44%] bg-ink relative overflow-hidden flex-col justify-between p-10">
        <CourtLines opacity={0.05} />

        <div
          className="absolute -right-[120px] -top-[80px] w-[320px] h-[320px] rounded-full blur-[10px]"
          style={{ background: "radial-gradient(closest-side, rgba(45,80,22,.55), transparent)" }}
        />

        {/* Logo */}
        <div className="relative z-10 flex items-center gap-2.5">
          <div className="w-[30px] h-[30px] rounded-md bg-tennis-lime grid place-items-center">
            <Mono className="text-ink font-extrabold text-[13px]">c/</Mono>
          </div>
          <span className="text-white font-bold text-base">CoachOS</span>
          <Mono className="ml-auto text-[10.5px] text-[#8a8377]">
            {tAuth("heroVersion")}
          </Mono>
        </div>

        {/* Hero copy — student-focused */}
        <div className="relative z-10">
          <SlashLabel className="text-tennis-lime mb-2.5">
            /leerlingen
          </SlashLabel>
          <h1 className="text-[34px] font-extrabold text-white leading-[1.05] tracking-tight mb-3">
            {t("heroLine1")}
            <br />
            <span className="text-tennis-lime">{t("heroLine2")}</span>
          </h1>
          <p className="text-[13px] text-[#a8a195] max-w-[300px] leading-relaxed">
            {t("tagline")}
          </p>
        </div>

        {/* Feature list */}
        <div className="relative z-10 p-4 bg-white/[.04] rounded-[10px] border border-white/[.08]">
          <div className="flex flex-col gap-3">
            {["Bevestig je plekken", "Zie wie je trainer is", "Zet lessen in je kalender"].map((x) => (
              <div key={x} className="flex items-center gap-2.5 text-[12.5px] text-[#e9e4db]">
                <div className="w-4 h-4 rounded-full bg-tennis-lime/20 grid place-items-center shrink-0">
                  <svg viewBox="0 0 24 24" width="9" height="9" fill="none" stroke="#D0FF14" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M5 12l5 5L20 7" />
                  </svg>
                </div>
                {x}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right panel — form (same structure as login) */}
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
            <SlashLabel>/login</SlashLabel>
            <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
              {t("title")}
            </h2>
            <p className="text-[12.5px] text-ink-3">
              {t("subtitle")}
            </p>
          </div>

          {/* Role segment — Leerling active */}
          <div className="flex border border-rule rounded-lg p-[3px] bg-canvas mb-5">
            <Link
              href="/login"
              className="flex-1 py-[7px] text-center text-[11.5px] text-ink-3"
            >
              {tAuth("roleCoach")}
            </Link>
            <div className="flex-1 py-[7px] text-center bg-white rounded-md text-[11.5px] font-semibold text-ink shadow-[0_1px_2px_rgba(0,0,0,.04)]">
              {tAuth("roleStudent")}
            </div>
          </div>

          {sent ? (
            <div className="px-4 py-6 rounded-lg bg-tennis-lime/10 border border-tennis-lime/30">
              <p className="text-tennis-green font-semibold mb-1">
                {t("sentTitle")}
              </p>
              <p className="text-ink-2 text-sm leading-relaxed mb-3">
                {t("sentBody")}
              </p>
              <button
                type="button"
                onClick={() => {
                  setSent(false);
                  setIsLoading(false);
                }}
                className="text-tennis-green text-sm font-semibold hover:underline cursor-pointer"
              >
                Opnieuw sturen
              </button>
            </div>
          ) : (
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3.5">
                <FormField
                  control={form.control}
                  name="email"
                  render={({ field }) => (
                    <FormItem>
                      <label className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]">
                        {t("email")}
                      </label>
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

                <Button
                  type="submit"
                  disabled={isLoading}
                  className="w-full h-[42px] bg-ink hover:bg-ink/90 text-white font-bold text-[13px] rounded-lg cursor-pointer"
                >
                  {isLoading ? t("loading") : `${t("submit")} →`}
                </Button>
              </form>
            </Form>
          )}

          <p className="mt-5 text-center text-[11.5px] text-ink-3">
            {t("trainerOrAdmin")}{" "}
            <Link
              href="/login"
              className="text-ink font-bold border-b border-tennis-lime"
            >
              {t("trainerLogin")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
