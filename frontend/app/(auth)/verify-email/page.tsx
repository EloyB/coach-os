"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";
import axios from "axios";

import { resendEmailVerification, verifyEmail } from "@/lib/api/auth";
import { getAuthUser, markEmailVerified } from "@/lib/auth";
import { AuthBrandPanel } from "@/components/auth/auth-brand-panel";
import { LogoMark } from "@/components/ui/logo-mark";
import { SlashLabel } from "@/components/ui/slash-label";
import { Spinner } from "@/components/ui/spinner";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";

const CODE_LENGTH = 6;
const RESEND_COOLDOWN_SECONDS = 60;

function extractError(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data: unknown = error.response.data;
    if (Array.isArray(data) && data.length > 0) return String(data[0]);
    if (typeof data === "string") return data;
  }
  return fallback;
}

function VerifyEmailForm() {
  const t = useTranslations("auth");
  const router = useRouter();
  const searchParams = useSearchParams();
  // Komt de gebruiker recht van de registratie, dan is er net een code verstuurd.
  const justSent = searchParams.get("sent") === "1";

  const [email, setEmail] = useState<string | null>(null);
  const [code, setCode] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [cooldown, setCooldown] = useState(justSent ? RESEND_COOLDOWN_SECONDS : 0);
  const codeInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const user = getAuthUser();
    if (!user) {
      router.replace("/login");
      return;
    }
    if (user.emailVerified !== false) {
      router.replace("/dashboard");
      return;
    }
    setEmail(user.email);

    // Wie later terugkeert (via de banner) heeft mogelijk een verlopen code. De server stuurt
    // dan zelf een verse; een nog geldige code blijft ongemoeid.
    if (justSent) return;
    let cancelled = false;
    resendEmailVerification({ onlyIfExpired: true })
      .then((sent) => {
        if (cancelled || !sent) return;
        setNotice(t("verifyFreshCodeSent"));
        setCooldown(RESEND_COOLDOWN_SECONDS);
      })
      .catch(() => {
        // Niet storend: de gebruiker kan nog altijd zelf een nieuwe code aanvragen.
      });
    return () => {
      cancelled = true;
    };
  }, [router, justSent, t]);

  useEffect(() => {
    if (cooldown <= 0) return;
    const timer = setTimeout(() => setCooldown((s) => s - 1), 1000);
    return () => clearTimeout(timer);
  }, [cooldown]);

  async function submit(value: string) {
    if (value.length !== CODE_LENGTH || isSubmitting) return;
    setIsSubmitting(true);
    setError(null);
    setNotice(null);
    try {
      await verifyEmail(value);
      markEmailVerified();
      router.push("/dashboard");
    } catch (err) {
      setError(extractError(err, t("verifyError")));
      setCode("");
      setIsSubmitting(false);
      // Het veld was tijdens het versturen uitgeschakeld en verloor de focus; geef die terug
      // zodat de gebruiker meteen opnieuw kan typen.
      requestAnimationFrame(() => codeInputRef.current?.focus());
    }
  }

  function onCodeChange(raw: string) {
    const digits = raw.replace(/\D/g, "").slice(0, CODE_LENGTH);
    setCode(digits);
    // Plakken of de laatste toets: meteen bevestigen, geen extra klik nodig.
    if (digits.length === CODE_LENGTH) void submit(digits);
  }

  async function resend() {
    if (cooldown > 0 || isResending) return;
    setIsResending(true);
    setError(null);
    setNotice(null);
    try {
      await resendEmailVerification();
      setNotice(t("verifyResent"));
      setCooldown(RESEND_COOLDOWN_SECONDS);
    } catch (err) {
      setError(extractError(err, t("verifyResendError")));
    } finally {
      setIsResending(false);
    }
  }

  if (!email) return null;

  return (
    <div className="min-h-screen flex">
      <AuthBrandPanel
        version={t("heroVersion")}
        eyebrow={t("registerEyebrow")}
        headline={[t("verifyHeroLine1"), t("verifyHeroLine2"), t("verifyHeroLine3")]}
        tagline={t("verifyTagline")}
      />

      <div className="w-full lg:w-[56%] flex items-center justify-center p-8 bg-paper">
        <div className="w-full max-w-[340px]">
          <div className="flex items-center gap-2 mb-10 lg:hidden">
            <LogoMark variant="green" />
            <span className="text-ink text-xl font-bold">CoachOS</span>
          </div>

          <div className="mb-5">
            <SlashLabel>{t("verifyEyebrow")}</SlashLabel>
            <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
              {t("verifyTitle")}
            </h2>
            <p className="text-[12.5px] text-ink-3 break-words">{t("verifySub", { email })}</p>
          </div>

          {error && (
            <div role="alert" className="mb-5 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              <p className="text-red-600 text-sm">{error}</p>
            </div>
          )}
          {notice && (
            <div role="status" className="mb-5 px-4 py-3 rounded-lg bg-green-600/10 border border-green-600/15">
              <p className="text-green-700 text-sm">{notice}</p>
            </div>
          )}

          <form
            onSubmit={(e) => {
              e.preventDefault();
              void submit(code);
            }}
            className="space-y-3.5"
          >
            <div className="grid gap-2">
              <Label
                htmlFor="verification-code"
                className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]"
              >
                {t("verifyCodeLabel")}
              </Label>
              <Input
                id="verification-code"
                ref={codeInputRef}
                value={code}
                onChange={(e) => onCodeChange(e.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                pattern="[0-9]*"
                maxLength={CODE_LENGTH}
                autoFocus
                disabled={isSubmitting}
                placeholder={"0".repeat(CODE_LENGTH)}
                className="h-14 bg-white border-rule rounded-lg text-ink text-center font-mono text-[26px] font-bold tracking-[0.45em] pl-[0.45em] placeholder:text-[#dcd8cf] focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green"
              />
            </div>

            <Button
              type="submit"
              disabled={isSubmitting || code.length !== CODE_LENGTH}
              className="w-full h-[42px] bg-ink hover:bg-ink/90 text-white font-bold text-[13px] rounded-lg cursor-pointer"
            >
              {isSubmitting ? (
                <span className="flex items-center gap-2">
                  <Spinner />
                  {t("loading")}
                </span>
              ) : (
                t("verifySubmit")
              )}
            </Button>
          </form>

          <p className="mt-4 text-center text-[11.5px] text-ink-3">
            {t("verifyNoMail")}{" "}
            {cooldown > 0 ? (
              <span>{t("verifyResendIn", { seconds: cooldown })}</span>
            ) : (
              <button
                type="button"
                onClick={resend}
                disabled={isResending}
                className="text-tennis-green font-semibold hover:underline cursor-pointer disabled:opacity-60"
              >
                {t("verifyResend")}
              </button>
            )}
          </p>

          <div className="mt-6 pt-5 border-t border-rule text-center">
            <Link href="/dashboard" className="text-[12px] text-ink font-bold border-b border-tennis-lime">
              {t("verifyLater")} →
            </Link>
            <p className="mt-2 text-[11px] text-ink-3 leading-relaxed">{t("verifyLaterHint")}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function VerifyEmailPage() {
  return (
    <Suspense>
      <VerifyEmailForm />
    </Suspense>
  );
}
