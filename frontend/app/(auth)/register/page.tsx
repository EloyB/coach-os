"use client";

import { useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { Check } from "lucide-react";
import * as z from "zod";
import axios from "axios";

import { register } from "@/lib/api/auth";
import { setToken, setAuthUser } from "@/lib/auth";
import { DATA_PROCESSING_AGREEMENT_URL, TERMS_URL } from "@/lib/legal";
import { AuthBrandPanel } from "@/components/auth/auth-brand-panel";
import { LogoMark } from "@/components/ui/logo-mark";
import { SlashLabel } from "@/components/ui/slash-label";
import { Spinner } from "@/components/ui/spinner";
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

const PASSWORD_MIN_LENGTH = 8;

const labelCls =
  "font-mono text-[11px] text-ink-2 font-semibold uppercase tracking-[0.06em]";
const inputCls =
  "h-10 bg-white border-rule rounded-lg text-ink text-[12.5px] placeholder:text-[#c5c0b6] focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green";
const consentLabelCls =
  "text-[12px] text-ink-2 font-normal leading-snug cursor-pointer";
const consentLinkCls =
  "text-tennis-green font-semibold underline underline-offset-2";

function PasswordChip({ label, met }: { label: string; met: boolean }) {
  return (
    <span
      className={`text-[10px] font-mono px-2 py-0.5 rounded-full inline-flex items-center gap-1 ${
        met ? "bg-green-600/10 text-green-700" : "bg-canvas text-ink-3"
      }`}
    >
      {met ? "✓" : "○"} {label}
    </span>
  );
}

function extractErrors(error: unknown, fallback: string): string[] {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data: unknown = error.response.data;
    if (Array.isArray(data)) return data.map(String);
    if (typeof data === "string") return [data];
  }
  return [fallback];
}

export default function RegisterPage() {
  const t = useTranslations("auth");
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  // Honeypot: bewust buiten react-hook-form en met een eigen veldnaam, zodat
  // wachtwoordmanagers het niet invullen maar bots wel.
  const [honeypot, setHoneypot] = useState("");

  const registerSchema = useMemo(
    () =>
      z
        .object({
          organizationName: z.string().trim().min(1, t("organizationNameRequired")).max(200),
          firstName: z.string().trim().min(1, t("firstNameRequired")).max(100),
          lastName: z.string().trim().min(1, t("lastNameRequired")).max(100),
          email: z.email(t("emailInvalid")),
          password: z
            .string()
            .min(PASSWORD_MIN_LENGTH, t("passwordRulesNotMet"))
            .regex(/[A-Z]/, t("passwordRulesNotMet"))
            .regex(/\d/, t("passwordRulesNotMet")),
          confirmPassword: z.string().min(1, t("confirmPasswordRequired")),
          acceptTerms: z.boolean().refine((v) => v, { message: t("acceptTermsRequired") }),
          acceptDpa: z.boolean().refine((v) => v, { message: t("acceptDpaRequired") }),
        })
        .refine((d) => d.password === d.confirmPassword, {
          message: t("passwordMismatch"),
          path: ["confirmPassword"],
        }),
    [t],
  );

  type RegisterFormValues = z.infer<typeof registerSchema>;

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      organizationName: "",
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      confirmPassword: "",
      acceptTerms: false,
      acceptDpa: false,
    },
  });

  const watchPassword = useWatch({ control: form.control, name: "password" });
  const pwdRules = [
    { label: t("pwdRuleLength"), met: watchPassword.length >= PASSWORD_MIN_LENGTH },
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
        acceptTerms: data.acceptTerms,
        acceptDataProcessingAgreement: data.acceptDpa,
        website: honeypot,
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
        emailVerified: response.emailVerified,
      });
      router.push(response.emailVerified === false ? "/verify-email?sent=1" : "/dashboard");
    } catch (error) {
      // 429 = rate limiter per IP of de globale registratie-noodrem; de limiter stuurt geen body mee.
      const tooManyRequests = axios.isAxiosError(error) && error.response?.status === 429;
      setErrors(tooManyRequests ? [t("registerTooManyRequests")] : extractErrors(error, t("registerError")));
      setIsLoading(false);
    }
  }

  const perks = [t("registerPerk1"), t("registerPerk2"), t("registerPerk3")];

  return (
    <div className="min-h-screen flex">
      <AuthBrandPanel
        version={t("heroVersion")}
        eyebrow={t("registerEyebrow")}
        headline={[t("registerHeroLine1"), t("registerHeroLine2"), t("registerHeroLine3")]}
        tagline={t("registerTagline")}
      >
        <ul className="p-4 bg-white/[.04] rounded-[10px] border border-white/[.08] space-y-2.5 m-0 list-none">
          {perks.map((perk) => (
            <li key={perk} className="flex items-start gap-2.5 text-[13px] text-[#e9e4db] font-medium">
              <span className="mt-0.5 w-4 h-4 rounded-full bg-tennis-lime grid place-items-center shrink-0">
                <Check className="w-3 h-3 text-ink" strokeWidth={3} aria-hidden="true" />
              </span>
              {perk}
            </li>
          ))}
        </ul>
      </AuthBrandPanel>

      {/* Right panel — form */}
      <div className="w-full lg:w-[56%] flex items-center justify-center p-8 bg-paper overflow-y-auto">
        <div className="w-full max-w-[400px] py-4">
          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-8 lg:hidden">
            <LogoMark variant="green" />
            <span className="text-ink text-xl font-bold">CoachOS</span>
          </div>

          <div className="mb-5">
            <SlashLabel>{t("registerStep")}</SlashLabel>
            <h2 className="text-2xl font-bold text-ink tracking-tight mt-1 mb-1.5">
              {t("registerHeadline")}
            </h2>
            <p className="text-[12.5px] text-ink-3">{t("registerSub")}</p>
          </div>

          {errors.length > 0 && (
            <div role="alert" className="mb-5 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              {errors.map((err, i) => (
                <p key={i} className="text-red-600 text-sm">
                  {err}
                </p>
              ))}
            </div>
          )}

          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3.5" noValidate>
              <FormField
                control={form.control}
                name="organizationName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className={labelCls}>{t("organizationName")}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        autoComplete="organization"
                        placeholder={t("organizationNamePlaceholder")}
                        className={inputCls}
                      />
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
                      <FormLabel className={labelCls}>{t("firstName")}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          autoComplete="given-name"
                          placeholder={t("firstNamePlaceholder")}
                          className={inputCls}
                        />
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
                      <FormLabel className={labelCls}>{t("lastName")}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          autoComplete="family-name"
                          placeholder={t("lastNamePlaceholder")}
                          className={inputCls}
                        />
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
                    <FormLabel className={labelCls}>{t("email")}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="email"
                        autoComplete="email"
                        placeholder={t("emailPlaceholder")}
                        className={inputCls}
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
                    <FormLabel className={labelCls}>{t("password")}</FormLabel>
                    <FormControl>
                      <Input {...field} type="password" autoComplete="new-password" className={inputCls} />
                    </FormControl>
                    <div className="flex flex-wrap gap-1.5 mt-1.5">
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
                    <FormLabel className={labelCls}>{t("confirmPassword")}</FormLabel>
                    <FormControl>
                      <Input {...field} type="password" autoComplete="new-password" className={inputCls} />
                    </FormControl>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              {/* Honeypot — onzichtbaar voor mensen en schermlezers. */}
              <div className="absolute -left-[9999px] h-0 w-0 overflow-hidden" aria-hidden="true">
                <input
                  type="text"
                  value={honeypot}
                  onChange={(e) => setHoneypot(e.target.value)}
                  name="company_website"
                  tabIndex={-1}
                  autoComplete="off"
                  data-1p-ignore
                  data-lpignore="true"
                />
              </div>

              <div className="space-y-2.5 pt-1">
                <FormField
                  control={form.control}
                  name="acceptTerms"
                  render={({ field }) => (
                    <FormItem>
                      <div className="flex items-start gap-2.5">
                        <FormControl>
                          <input
                            type="checkbox"
                            name={field.name}
                            ref={field.ref}
                            checked={field.value}
                            onChange={(e) => field.onChange(e.target.checked)}
                            onBlur={field.onBlur}
                            className="mt-0.5 h-4 w-4 shrink-0 accent-tennis-green cursor-pointer"
                          />
                        </FormControl>
                        <FormLabel className={consentLabelCls}>
                          <span>
                            {t.rich("acceptTerms", {
                              link: (chunks) => (
                                <a href={TERMS_URL} target="_blank" rel="noopener noreferrer" className={consentLinkCls}>
                                  {chunks}
                                </a>
                              ),
                            })}
                          </span>
                        </FormLabel>
                      </div>
                      <FormMessage className="text-xs" />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="acceptDpa"
                  render={({ field }) => (
                    <FormItem>
                      <div className="flex items-start gap-2.5">
                        <FormControl>
                          <input
                            type="checkbox"
                            name={field.name}
                            ref={field.ref}
                            checked={field.value}
                            onChange={(e) => field.onChange(e.target.checked)}
                            onBlur={field.onBlur}
                            className="mt-0.5 h-4 w-4 shrink-0 accent-tennis-green cursor-pointer"
                          />
                        </FormControl>
                        <FormLabel className={consentLabelCls}>
                          <span>
                            {t.rich("acceptDpa", {
                              link: (chunks) => (
                                <a
                                  href={DATA_PROCESSING_AGREEMENT_URL}
                                  target="_blank"
                                  rel="noopener noreferrer"
                                  className={consentLinkCls}
                                >
                                  {chunks}
                                </a>
                              ),
                            })}
                          </span>
                        </FormLabel>
                      </div>
                      <FormMessage className="text-xs" />
                    </FormItem>
                  )}
                />
              </div>

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
                  t("registerCta")
                )}
              </Button>
            </form>
          </Form>

          <p className="mt-5 text-center text-[11.5px] text-ink-3">
            {t("hasAccount")}{" "}
            <Link href="/login" className="text-ink font-bold border-b border-tennis-lime">
              {t("hasAccountCta")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
