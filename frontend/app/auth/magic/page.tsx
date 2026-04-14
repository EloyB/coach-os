"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";

import { redeemMagicLink } from "@/lib/api/auth";
import { setToken, setAuthUser } from "@/lib/auth";

function MagicRedeem() {
  const t = useTranslations("studentAuth");
  const router = useRouter();
  const params = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = params.get("token");
    if (!token) {
      setError(t("invalidLink"));
      return;
    }
    redeemMagicLink(token)
      .then((res) => {
        setToken(res.token);
        setAuthUser({ email: res.email, role: res.role });
        router.replace("/student/lessons");
      })
      .catch(() => setError(t("invalidLink")));
  }, [params, router, t]);

  return (
    <div className="min-h-screen flex items-center justify-center p-8 bg-[#FAFAF8]">
      <div className="max-w-sm w-full text-center">
        {error ? (
          <>
            <h1 className="text-xl font-semibold text-gray-900 mb-2">
              {t("invalidLinkTitle")}
            </h1>
            <p className="text-gray-500 mb-6">{error}</p>
            <Link
              href="/student-login"
              className="inline-block px-4 py-2 rounded-lg bg-tennis-green text-white font-semibold"
            >
              {t("requestNew")}
            </Link>
          </>
        ) : (
          <p className="text-gray-500">{t("signingIn")}</p>
        )}
      </div>
    </div>
  );
}

export default function MagicPage() {
  return (
    <Suspense>
      <MagicRedeem />
    </Suspense>
  );
}
