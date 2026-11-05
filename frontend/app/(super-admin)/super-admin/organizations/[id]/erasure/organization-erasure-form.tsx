"use client";
import { useState } from "react";
import { useTranslations } from "next-intl";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  processOrganizationErasure,
  type OrganizationErasureResult,
} from "@/lib/api/offboarding";
export function OrganizationErasureForm({
  organizationId,
}: {
  organizationId: string;
}) {
  const t = useTranslations("organizationPrivacy");
  const [admin, setAdmin] = useState("");
  const [verified, setVerified] = useState(false);
  const [name, setName] = useState("");
  const [preview, setPreview] = useState<OrganizationErasureResult | null>(
    null,
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  async function process(apply = false) {
    setBusy(true);
    setError(false);
    try {
      if (
        !z.string().uuid().safeParse(admin).success ||
        !z.string().uuid().safeParse(organizationId).success ||
        !verified
      )
        throw new Error("invalid_request");
      const result = await processOrganizationErasure(organizationId, {
        early: true,
        requestingAdminId: admin,
        verifiedRequest: verified,
        ...(apply
          ? {
              apply: true,
              confirmation: name,
              previewHash: preview?.previewHash ?? undefined,
            }
          : {}),
      });
      setPreview(result);
    } catch {
      setError(true);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="mx-auto max-w-2xl space-y-5">
      <header>
        <h1 className="text-2xl font-semibold text-ink">{t("erasureTitle")}</h1>
        <p className="mt-2 text-sm text-ink-3">{t("scope")}</p>
      </header>
      <div className="rounded-lg border border-rule bg-paper p-5 space-y-4">
        <p className="text-sm text-ink-3">{t("gates")}</p>
        <label className="block text-sm" htmlFor="requesting-admin">
          {t("requestingAdmin")}
        </label>
        <Input
          id="requesting-admin"
          value={admin}
          onChange={(e) => {
            setAdmin(e.target.value);
            setPreview(null);
          }}
        />
        <label className="flex gap-2 items-start text-sm">
          <input
            type="checkbox"
            checked={verified}
            onChange={(e) => {
              setVerified(e.target.checked);
              setPreview(null);
            }}
          />
          {t("verified")}
        </label>
        <Button
          onClick={() => process()}
          disabled={
            busy || !verified || !z.string().uuid().safeParse(admin).success
          }
        >
          {t("review")}
        </Button>
        {error && (
          <p role="alert" className="text-destructive">
            {t("erasureError")}
          </p>
        )}
        {preview?.deleted ? (
          <p role="status">{t("deleted")}</p>
        ) : (
          preview && (
            <div className="space-y-4 border-t border-rule pt-4">
              <p>
                {t("counts", {
                  enrollments: preview.enrollmentCount,
                  camps: preview.campEnrollmentCount,
                  members: preview.membershipCount,
                })}
              </p>
              <ul>
                {preview.holds.map((hold) => (
                  <li key={hold} className="text-sm text-destructive">
                    {t.has(`holds.${hold}`)
                      ? t(`holds.${hold}`)
                      : t("holdGeneric")}
                  </li>
                ))}
              </ul>
              {preview.eligible && (
                <>
                  <label className="block text-sm" htmlFor="exact-name">
                    {t("exactName")}
                  </label>
                  <Input
                    id="exact-name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    maxLength={250}
                  />
                  <Button
                    variant="destructive"
                    disabled={busy || !name.trim()}
                    onClick={() => process(true)}
                  >
                    {t("erase")}
                  </Button>
                </>
              )}
            </div>
          )
        )}
      </div>
    </section>
  );
}
