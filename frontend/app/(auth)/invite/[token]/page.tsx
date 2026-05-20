"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import * as z from "zod";

import { acceptInvite, validateInvite } from "@/lib/api/auth";
import { setToken, setAuthUser, setSuperAdminCookie } from "@/lib/auth";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
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
import { Spinner } from "@/components/ui/spinner";

const schema = z
  .object({
    password: z.string().min(8, "Wachtwoord moet minimaal 8 karakters zijn"),
    confirmPassword: z.string().min(1, "Bevestig je wachtwoord"),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Wachtwoorden komen niet overeen",
    path: ["confirmPassword"],
  });

type FormValues = z.infer<typeof schema>;

export default function InvitePage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = use(params);
  const t = useTranslations("auth");
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [validationState, setValidationState] = useState<
    "checking" | "valid" | "invalid"
  >("checking");
  const [invalidMessage, setInvalidMessage] = useState<string>(
    "Deze uitnodigingslink is niet langer geldig.",
  );

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        await validateInvite(token);
        if (!cancelled) setValidationState("valid");
      } catch (error) {
        if (cancelled) return;
        const messages = getAxiosErrorMessages(
          error,
          "Deze uitnodigingslink is niet langer geldig.",
        );
        setInvalidMessage(
          messages[0] ?? "Deze uitnodigingslink is niet langer geldig.",
        );
        setValidationState("invalid");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { password: "", confirmPassword: "" },
  });

  async function onSubmit(data: FormValues) {
    setIsLoading(true);
    setErrors([]);
    try {
      const response = await acceptInvite({ token, password: data.password });
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
      router.push(
        response.isSuperAdmin ? "/super-admin/dashboard" : "/dashboard",
      );
    } catch (error) {
      setErrors(
        getAxiosErrorMessages(error, "Er ging iets mis. Probeer het opnieuw."),
      );
    } finally {
      setIsLoading(false);
    }
  }

  const features = [
    t("inviteFeature1"),
    t("inviteFeature2"),
    t("inviteFeature3"),
  ];

  return (
    <div className="min-h-screen flex">
      {/* Left panel — ink branding (matches login) */}
      <div className="hidden lg:flex lg:w-[44%] bg-ink relative overflow-hidden flex-col justify-between p-10">
        <CourtLines opacity={0.05} />

        {/* Subtle green glow */}
        <div
          className="absolute -right-[120px] -top-[80px] w-[320px] h-[320px] rounded-full blur-[10px]"
          style={{
            background:
              "radial-gradient(closest-side, rgba(45,80,22,.55), transparent)",
          }}
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
            {t("inviteEyebrow")}
          </SlashLabel>
          <h1 className="text-[34px] font-extrabold text-white leading-[1.05] tracking-tight mb-3">
            {t("inviteHeroLine1")}
            <br />
            <span className="text-tennis-lime">{t("inviteHeroLine2")}</span>
          </h1>
          <p className="text-[13px] text-[#a8a195] max-w-[300px] leading-relaxed">
            {t("inviteTagline")}
          </p>
        </div>

        {/* Feature list */}
        <div className="relative z-10 p-4 bg-white/[.04] rounded-[10px] border border-white/[.08]">
          <div className="flex flex-col gap-3">
            {features.map((feature) => (
              <div
                key={feature}
                className="flex items-center gap-2.5 text-[12.5px] text-[#e9e4db]"
              >
                <div className="w-4 h-4 rounded-full bg-tennis-lime/20 grid place-items-center shrink-0">
                  <svg
                    viewBox="0 0 24 24"
                    width="9"
                    height="9"
                    fill="none"
                    stroke="#D0FF14"
                    strokeWidth="3"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M5 12l5 5L20 7" />
                  </svg>
                </div>
                {feature}
              </div>
            ))}
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
            <SlashLabel>/uitnodiging</SlashLabel>
            <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
              {t("inviteFormTitle")}
            </h2>
            <p className="text-[12.5px] text-ink-3">{t("inviteFormSub")}</p>
          </div>

          {validationState === "checking" && (
            <div className="flex items-center justify-center py-10">
              <Spinner />
            </div>
          )}

          {validationState === "invalid" && (
            <div className="space-y-4">
              <div className="px-4 py-3 rounded-lg bg-red-50 border border-red-100">
                <p className="text-red-600 text-sm">{invalidMessage}</p>
              </div>
              <p className="text-gray-500 text-sm">
                Deze link is verlopen of al gebruikt. Log in met je wachtwoord
                of vraag een nieuwe uitnodiging aan.
              </p>
              <Link
                href="/login"
                className="inline-flex items-center justify-center w-full h-11 bg-tennis-green hover:bg-tennis-green/90 text-white font-semibold rounded-lg transition-colors"
              >
                Naar inloggen
              </Link>
            </div>
          )}

          {/* Error banner */}
          {validationState === "valid" && errors.length > 0 && (
            <div className="mb-6 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              {errors.map((err, i) => (
                <p key={i} className="text-red-600 text-sm">
                  {err}
                </p>
              ))}
            </div>
          )}

          {validationState === "valid" && (
            <Form {...form}>
              <form
                onSubmit={form.handleSubmit(onSubmit)}
                className="space-y-3.5"
              >
                <FormField
                  control={form.control}
                  name="password"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]">
                        {t("password")}
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
                      {t("inviteSubmitting")}
                    </span>
                  ) : (
                    <>{t("inviteSubmit")} →</>
                  )}
                </Button>
              </form>
            </Form>
          )}
        </div>
      </div>
    </div>
  );
}
