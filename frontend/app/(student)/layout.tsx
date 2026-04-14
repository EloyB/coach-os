"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";

import { clearAuth, getAuthUser } from "@/lib/auth";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";

export default function StudentLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const t = useTranslations("studentPortal");
  const [email, setEmail] = useState<string | null>(null);

  useEffect(() => {
    const user = getAuthUser();
    if (!user || user.role !== "Student") {
      router.replace("/student-login");
      return;
    }
    setEmail(user.email);
  }, [router]);

  function logout() {
    clearAuth();
    router.replace("/student-login");
  }

  return (
    <div className="min-h-screen bg-[#FAFAF8]">
      <header className="bg-tennis-green text-white">
        <div className="max-w-4xl mx-auto px-6 py-4 flex items-center justify-between">
          <Link href="/student/lessons" className="flex items-center gap-2">
            <div className="w-7 h-7 rounded-full bg-tennis-lime flex items-center justify-center">
              <TennisBallIcon className="w-4 h-4" strokeWidth={2} />
            </div>
            <span className="text-lg font-bold tracking-tight">
              Coach<span className="text-tennis-lime">OS</span>
            </span>
          </Link>
          <div className="flex items-center gap-4 text-sm">
            {email && <span className="hidden sm:inline opacity-80">{email}</span>}
            <button
              onClick={logout}
              className="px-3 py-1.5 rounded-md bg-white/10 hover:bg-white/20 transition-colors cursor-pointer"
            >
              {t("logout")}
            </button>
          </div>
        </div>
      </header>
      <main className="max-w-4xl mx-auto px-6 py-8">{children}</main>
    </div>
  );
}
