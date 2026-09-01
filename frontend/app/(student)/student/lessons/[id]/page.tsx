"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";

import { getMyLesson } from "@/lib/api/student";
import { Card, CardContent } from "@/components/ui/card";

// Backend-conventie: 0=maandag ... 6=zondag (zie CoachOS.Application/LessonSerie/LessonSerieService.cs).
const DAYS_NL = ["maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag", "zondag"];

export default function StudentLessonDetailPage() {
  const t = useTranslations("studentPortal");
  const params = useParams<{ id: string }>();
  const { data, isLoading, isError } = useQuery({
    queryKey: ["student", "lesson", params.id],
    queryFn: () => getMyLesson(params.id),
  });

  if (isLoading) return <p className="text-gray-500">{t("loading")}</p>;
  if (isError || !data) return <p className="text-red-600">{t("loadError")}</p>;

  return (
    <div>
      <Link
        href="/student/lessons"
        className="text-sm text-tennis-green font-semibold hover:underline"
      >
        ← {t("back")}
      </Link>

      <div className="mb-6 mt-4">
        <h1 className="text-2xl font-bold text-gray-900">{data.seriesName}</h1>
        <p className="text-gray-500 text-sm mt-1">
          {data.seriesStartDate} → {data.seriesEndDate}
        </p>
      </div>

      <div className="grid sm:grid-cols-2 gap-4">
        <DetailCard label={t("when")}>
          {DAYS_NL[data.dayOfWeek]} {data.startTime} — {data.endTime}
        </DetailCard>
        <DetailCard label={t("court")}>{data.courtName ?? "—"}</DetailCard>
        <DetailCard label={t("trainer")}>{data.trainerName ?? "—"}</DetailCard>
        <DetailCard label={t("status")}>{data.status}</DetailCard>
        <DetailCard label={t("groupSize")}>
          {data.isGroup ? `${data.groupSize} ${t("people")}` : t("solo")}
        </DetailCard>
        <DetailCard label={t("price")}>€ {data.price.toFixed(2)}</DetailCard>
        <DetailCard label={t("paymentStatus")}>
          {data.paymentStatus ?? t("notPaid")}
        </DetailCard>
      </div>
    </div>
  );
}

function DetailCard({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <Card>
      <CardContent className="py-4">
        <p className="text-xs uppercase tracking-wide text-gray-400 font-semibold">
          {label}
        </p>
        <p className="text-base font-medium text-gray-900 mt-1">{children}</p>
      </CardContent>
    </Card>
  );
}
