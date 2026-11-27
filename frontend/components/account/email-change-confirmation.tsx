"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import axios from "axios";
import { Loader2 } from "lucide-react";
import { confirmEmailChange, type ConfirmEmailChange } from "@/lib/api/auth";
import { setAuthUser, setSuperAdminCookie, setToken } from "@/lib/auth";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";

export function EmailChangeConfirmation({ challenge, onCancel }: {
  challenge: ConfirmEmailChange;
  onCancel: () => void;
}) {
  const t = useTranslations("account");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    if (pending) return;
    setPending(true);
    setError(null);
    try {
      const response = await confirmEmailChange(challenge);
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
        headTrainerClubIds: response.headTrainerClubIds,
        emailVerified: response.emailVerified,
      });
      // Full navigation refreshes the shell too: useAuthUser does not subscribe
      // to storage changes. The consumed challenge stays in memory only.
      window.location.replace("/dashboard/account?emailChanged=1");
    } catch (error) {
      setError(t(axios.isAxiosError(error) && error.response?.status === 429 ? "rateLimited" : "confirmError"));
      setPending(false);
    }
  }

  return (
    <Card className="bg-paper border-rule shadow-none">
      <CardHeader className="border-b border-rule">
        <CardTitle className="text-base text-ink"><h2>{t("confirmTitle")}</h2></CardTitle>
        <CardDescription>{t("globalScope")}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-ink-2">{t("confirmDescription")}</p>
        <p className="font-semibold text-ink break-all">{challenge.newEmail}</p>
        <p className="text-xs text-ink-3">{t("oldEmailRemains")}</p>
        {error && <p role="alert" className="text-sm text-destructive">{error}</p>}
        <div className="flex flex-col sm:flex-row gap-2">
          <Button onClick={confirm} disabled={pending} className="bg-tennis-green hover:bg-tennis-green/90 text-white">
            {pending && <Loader2 className="animate-spin" aria-hidden="true" />}
            {t(pending ? "confirming" : "confirm")}
          </Button>
          <Button variant="outline" onClick={onCancel} disabled={pending}>{t("cancel")}</Button>
        </div>
      </CardContent>
    </Card>
  );
}
