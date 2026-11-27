"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import axios from "axios";
import { Mail, Loader2 } from "lucide-react";
import { useAuthUser } from "@/lib/hooks/use-auth-user";
import { requestEmailChange, type ConfirmEmailChange } from "@/lib/api/auth";
import { EmailChangeConfirmation } from "@/components/account/email-change-confirmation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form";

// The dashboard shell only mounts its children after client authentication.
// Keep the challenge in component memory, never in storage or a redirect URL.
function readChallenge(): ConfirmEmailChange | "invalid" | null {
  if (typeof window === "undefined" || !window.location.hash) return null;
  const params = new URLSearchParams(window.location.hash.slice(1));
  const newEmail = params.get("email");
  const token = params.get("token");
  if (!newEmail || !token || !z.email().max(256).safeParse(newEmail).success) return "invalid";
  return { newEmail, token };
}

export default function AccountPage() {
  const t = useTranslations("account");
  const user = useAuthUser();
  const [challenge, setChallenge] = useState(readChallenge);
  const [changed] = useState(() => typeof window !== "undefined" && new URLSearchParams(window.location.search).get("emailChanged") === "1");

  useEffect(() => {
    function cleanFragment() {
      if (window.location.hash) window.history.replaceState(window.history.state, "", window.location.pathname + window.location.search);
    }
    function onHashChange() {
      setChallenge(readChallenge());
      cleanFragment();
    }
    cleanFragment();
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);
  const [sentTo, setSentTo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const schema = z.object({
    newEmail: z.email(t("invalidEmail")).max(256, t("invalidEmail")),
    currentPassword: z.string().min(1, t("passwordRequired")),
  });
  const form = useForm<z.infer<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: { newEmail: "", currentPassword: "" },
  });

  async function onSubmit(values: z.infer<typeof schema>) {
    setError(null);
    setSentTo(null);
    try {
      await requestEmailChange(values);
      setSentTo(values.newEmail);
      form.resetField("currentPassword");
    } catch (error) {
      setError(t(axios.isAxiosError(error) && error.response?.status === 429 ? "rateLimited" : "requestError"));
    }
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">{t("title")}</h1>
        <p className="text-sm text-ink-3 mt-1">{t("description")}</p>
      </div>
      {changed && <p role="status" className="rounded-lg border border-tennis-green/20 bg-tennis-green/5 p-4 text-sm font-medium text-tennis-green">{t("confirmSuccess")}</p>}
      {challenge === "invalid" && <p role="alert" className="text-sm text-destructive">{t("confirmError")}</p>}
      {challenge && challenge !== "invalid" ? (
        <EmailChangeConfirmation challenge={challenge} onCancel={() => setChallenge(null)} />
      ) : <Card className="bg-paper border-rule shadow-none">
        <CardHeader className="border-b border-rule">
          <CardTitle className="flex items-center gap-2 text-base text-ink"><Mail size={18} className="text-tennis-green" />{t("emailTitle")}</CardTitle>
          <CardDescription>{t("globalScope")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <div>
            <p className="text-xs font-medium text-ink-3">{t("currentEmail")}</p>
            <p className="text-sm font-medium text-ink break-all mt-1">{user?.email}</p>
          </div>
          {sentTo && <div role="status" className="rounded-lg border border-tennis-green/20 bg-tennis-green/5 p-4 text-sm space-y-1">
            <p className="font-semibold text-tennis-green">{t("requestSuccess")}</p>
            <p className="text-ink-2 break-words">{t("sentTo", { email: sentTo })}</p>
            <p className="text-ink-2">{t("oldEmailRemains")}</p>
            <p className="text-xs text-ink-3">{t("resendHint")}</p>
          </div>}
          <Form {...form}>
            <form noValidate onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField control={form.control} name="newEmail" render={({ field }) => (
                <FormItem><FormLabel>{t("newEmail")}</FormLabel><FormControl><Input {...field} type="email" autoComplete="email" className="bg-white border-rule" /></FormControl><FormMessage /></FormItem>
              )} />
              <FormField control={form.control} name="currentPassword" render={({ field }) => (
                <FormItem><FormLabel>{t("currentPassword")}</FormLabel><FormControl><Input {...field} type="password" autoComplete="current-password" className="bg-white border-rule" /></FormControl><FormMessage /></FormItem>
              )} />
              {error && <p role="alert" className="text-sm text-destructive">{error}</p>}
              <Button type="submit" disabled={form.formState.isSubmitting} className="bg-tennis-green hover:bg-tennis-green/90 text-white w-full sm:w-auto">
                {form.formState.isSubmitting && <Loader2 className="animate-spin" aria-hidden="true" />}
                {t(form.formState.isSubmitting ? "sending" : "send")}
              </Button>
              {!sentTo && <p className="text-xs text-ink-3">{t("oldEmailRemains")}</p>}
            </form>
          </Form>
        </CardContent>
      </Card>}
    </div>
  );
}
