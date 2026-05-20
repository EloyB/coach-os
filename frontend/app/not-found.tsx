import Link from "next/link";
import { getTranslations } from "next-intl/server";

export default async function NotFound() {
  const t = await getTranslations("notFound");

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#FAFAF8] px-4">
      <div className="max-w-md w-full text-center">
        <div className="text-tennis-green text-7xl font-bold tracking-tight">
          {t("subtitle")}
        </div>
        <h1 className="mt-4 text-2xl font-bold text-gray-900 tracking-tight">
          {t("title")}
        </h1>
        <p className="mt-3 text-sm text-gray-500 leading-relaxed">
          {t("description")}
        </p>
        <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
          <Link
            href="/dashboard"
            className="inline-flex items-center justify-center px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
          >
            {t("backToDashboard")}
          </Link>
          <Link
            href="/"
            className="inline-flex items-center justify-center px-5 py-2.5 border border-gray-200 text-sm font-medium text-gray-700 rounded-lg hover:bg-white transition-colors"
          >
            {t("backToHome")}
          </Link>
        </div>
      </div>
    </div>
  );
}
