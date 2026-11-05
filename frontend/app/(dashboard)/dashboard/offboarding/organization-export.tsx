"use client";
import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import { getAuthUser } from "@/lib/auth";
import { downloadOrganizationExport } from "@/lib/api/offboarding";
export function OrganizationExport() {
  const [isAdmin, setIsAdmin] = useState(false);
  useEffect(() => setIsAdmin(getAuthUser()?.role === "Admin"), []);
  const t = useTranslations("organizationPrivacy");
  const [busy, setBusy] = useState(false);
  const [state, setState] = useState<"done" | "error" | null>(null);
  async function download() {
    setBusy(true);
    setState(null);
    try {
      const blob = await downloadOrganizationExport();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "coachos-club-export.zip";
      a.click();
      URL.revokeObjectURL(url);
      setState("done");
    } catch {
      setState("error");
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="mx-auto max-w-2xl space-y-5">
      <header>
        <h1 className="text-2xl font-semibold text-ink">{t("exportTitle")}</h1>
        <p className="mt-2 text-sm text-ink-3">{t("exportDescription")}</p>
      </header>
      <div className="rounded-lg border border-rule bg-paper p-5 space-y-4">
        <p className="text-sm">{t("exportPrivacy")}</p>
        {isAdmin ? (
          <Button onClick={download} disabled={busy}>
            {t(busy ? "busy" : "download")}
          </Button>
        ) : (
          <p>{t("adminOnly")}</p>
        )}
        {state === "done" && <p role="status">{t("downloadStarted")}</p>}
        {state === "error" && (
          <p role="alert" className="text-destructive">
            {t("exportError")}
          </p>
        )}
      </div>
    </section>
  );
}
