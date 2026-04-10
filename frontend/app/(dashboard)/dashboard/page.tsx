"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  Map,
  BookOpen,
  Users,
  Plus,
  Calendar,
  CreditCard,
  TrendingUp,
  Clock,
} from "lucide-react";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { WelcomeHeader } from "@/components/dashboard/welcome-header";
import { getDashboardSummary } from "@/lib/api/dashboard";

function formatDate(dateStr: string): string {
  const [year, month, day] = dateStr.split("-");
  return `${day}/${month}/${year}`;
}

function StatSkeleton() {
  return (
    <div className="bg-white rounded-xl p-5 border-l-4 border-l-gray-200 shadow-sm shadow-gray-100 animate-pulse">
      <div className="flex items-start justify-between">
        <div>
          <div className="h-3 bg-gray-200 rounded w-24 mb-3" />
          <div className="h-8 bg-gray-200 rounded w-12" />
        </div>
        <div className="p-2 rounded-lg bg-gray-100 w-8 h-8" />
      </div>
    </div>
  );
}

const quickActions = [
  { label: "Nieuwe les", href: "/dashboard/lessons/new", icon: Plus },
  { label: "Leerling toevoegen", href: "/dashboard/lessons", icon: Users },
];

export default function DashboardPage() {
  const { data: summary, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: getDashboardSummary,
  });

  const stats = [
    {
      label: "Actieve lesreeksen",
      value: summary?.activeSeriesCount ?? 0,
      icon: BookOpen,
      textColor: "text-tennis-green",
      borderColor: "border-l-tennis-green",
    },
    {
      label: "Lessen deze week",
      value: summary?.lessonsThisWeekCount ?? 0,
      icon: Calendar,
      textColor: "text-blue-600",
      borderColor: "border-l-blue-500",
    },
    {
      label: "Inschrijvingen",
      value: summary?.totalEnrollmentCount ?? 0,
      icon: Users,
      textColor: "text-violet-600",
      borderColor: "border-l-violet-500",
    },
    {
      label: "Trainers",
      value: summary?.activeTrainerCount ?? 0,
      icon: CreditCard,
      textColor: "text-amber-600",
      borderColor: "border-l-amber-500",
    },
  ];

  return (
    <>
      <WelcomeHeader />

      {/* Stat cards */}
      <div className="grid grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        {isLoading
          ? Array.from({ length: 4 }).map((_, i) => <StatSkeleton key={i} />)
          : stats.map(({ label, value, icon: Icon, textColor, borderColor }) => (
              <div
                key={label}
                className={`bg-white rounded-xl p-5 border-l-4 ${borderColor} shadow-sm shadow-gray-100`}
              >
                <div className="flex items-start justify-between">
                  <div>
                    <p className="text-xs text-gray-400 font-medium mb-2 uppercase tracking-wide">
                      {label}
                    </p>
                    <p className={`text-3xl font-bold ${textColor} leading-none`}>
                      {value}
                    </p>
                  </div>
                  <div className="p-2 rounded-lg bg-gray-50">
                    <Icon size={16} className="text-gray-400" />
                  </div>
                </div>
              </div>
            ))}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Upcoming lessons */}
        <div className="xl:col-span-2 bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
          <div className="px-6 py-5 border-b border-gray-50 flex items-center justify-between">
            <h2 className="font-semibold text-gray-800 text-sm">
              Komende lessen
            </h2>
            <Link
              href="/dashboard/lessons/new"
              className="flex items-center gap-1.5 text-xs font-medium text-tennis-green hover:text-tennis-green/80 transition-colors"
            >
              <Plus size={13} />
              Nieuwe les
            </Link>
          </div>

          {isLoading && (
            <div className="p-6 space-y-3 animate-pulse">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-12 bg-gray-100 rounded-lg" />
              ))}
            </div>
          )}

          {!isLoading && (!summary?.upcomingLessons || summary.upcomingLessons.length === 0) && (
            <div className="flex flex-col items-center justify-center py-16 px-6 text-center">
              <div className="w-16 h-16 rounded-full bg-tennis-green/8 flex items-center justify-center mb-5">
                <TennisBallEmptyIcon className="w-8 h-8" />
              </div>
              <p className="text-gray-700 font-semibold text-sm mb-1">
                Nog geen lessen gepland
              </p>
              <p className="text-gray-400 text-xs max-w-60 leading-relaxed mb-6">
                Plan je eerste les en begin met het bijhouden van je schema.
              </p>
              <Link
                href="/dashboard/lessons/new"
                className="inline-flex items-center gap-2 px-4 py-2.5 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
              >
                <Plus size={13} />
                Plan een les
              </Link>
            </div>
          )}

          {!isLoading && summary?.upcomingLessons && summary.upcomingLessons.length > 0 && (
            <div className="divide-y divide-gray-50">
              {summary.upcomingLessons.map((lesson) => (
                <div key={lesson.id} className="px-6 py-4 flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 rounded-lg bg-tennis-green/10 flex items-center justify-center">
                      <Calendar size={16} className="text-tennis-green" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-900">{lesson.seriesName}</p>
                      <p className="text-xs text-gray-400">{lesson.trainerName} &middot; {lesson.courtName}</p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium text-gray-700">{formatDate(lesson.date)}</p>
                    <p className="text-xs text-gray-400 flex items-center gap-1 justify-end">
                      <Clock size={10} />
                      {lesson.startTime} – {lesson.endTime}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Quick actions */}
        <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
          <div className="px-6 py-5 border-b border-gray-50">
            <h2 className="font-semibold text-gray-800 text-sm">
              Snelle acties
            </h2>
          </div>
          <div className="p-4 space-y-2">
            {quickActions.map(({ label, href, icon: Icon }) => (
              <Link
                key={href}
                href={href}
                className="flex items-center gap-3 p-3.5 rounded-lg hover:bg-gray-50 transition-colors group"
              >
                <div className="w-8 h-8 rounded-lg bg-tennis-green/10 flex items-center justify-center shrink-0 group-hover:bg-tennis-green/20 transition-colors">
                  <Icon size={14} className="text-tennis-green" />
                </div>
                <span className="text-sm font-medium text-gray-700 group-hover:text-gray-900 transition-colors">
                  {label}
                </span>
                <svg
                  className="ml-auto w-4 h-4 text-gray-300 group-hover:text-gray-400 transition-colors"
                  viewBox="0 0 16 16"
                  fill="none"
                  aria-hidden="true"
                >
                  <path
                    d="M6 4l4 4-4 4"
                    stroke="currentColor"
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              </Link>
            ))}

            <div className="mt-4 pt-4 border-t border-gray-50">
              <div className="rounded-lg bg-tennis-beige/60 p-3.5">
                <p className="text-xs font-semibold text-tennis-green mb-1">
                  Tip
                </p>
                <p className="text-xs text-gray-500 leading-relaxed">
                  Maak eerst je tennisclubs aan voordat je lessen plant.
                </p>
                <Link
                  href="/dashboard/settings"
                  className="inline-block mt-2 text-xs font-semibold text-tennis-green hover:underline"
                >
                  Instellingen →
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
