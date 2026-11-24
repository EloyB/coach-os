"use client";

import { useTranslations } from "next-intl";
import { ENROLLMENT_TERMS_URL, PRIVACY_URL } from "@/lib/legal";

const linkCls = "underline underline-offset-2 hover:text-gray-600";

/** Meldt onder de inschrijfknop dat inschrijven de voorwaarden aanvaardt (art. 2.2 spelersvoorwaarden). */
export function EnrollmentLegalNotice() {
  const t = useTranslations("legal");

  return (
    <p className="text-xs text-gray-400 text-center mt-3">
      {t.rich("enrollmentNotice", {
        terms: (chunks) => (
          <a href={ENROLLMENT_TERMS_URL} target="_blank" rel="noopener noreferrer" className={linkCls}>
            {chunks}
          </a>
        ),
        privacy: (chunks) => (
          <a href={PRIVACY_URL} target="_blank" rel="noopener noreferrer" className={linkCls}>
            {chunks}
          </a>
        ),
      })}
    </p>
  );
}
