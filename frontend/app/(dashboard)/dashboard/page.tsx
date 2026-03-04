import Link from "next/link";
import {
  Map,
  BookOpen,
  Users,
  Plus,
  Calendar,
  CreditCard,
  TrendingUp,
} from "lucide-react";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";

const stats = [
  {
    label: "Actieve banen",
    value: "0",
    icon: Map,
    textColor: "text-tennis-green",
    borderColor: "border-l-tennis-green",
  },
  {
    label: "Lessen deze week",
    value: "0",
    icon: Calendar,
    textColor: "text-blue-600",
    borderColor: "border-l-blue-500",
  },
  {
    label: "Leerlingen",
    value: "0",
    icon: Users,
    textColor: "text-violet-600",
    borderColor: "border-l-violet-500",
  },
  {
    label: "Openstaande betalingen",
    value: "€0",
    icon: CreditCard,
    textColor: "text-amber-600",
    borderColor: "border-l-amber-500",
  },
];

const quickActions = [
  { label: "Nieuwe les", href: "/dashboard/lessons/new", icon: Plus },
  { label: "Nieuwe baan", href: "/dashboard/courts/new", icon: Map },
  { label: "Leerling toevoegen", href: "/dashboard/students/new", icon: Users },
];

export default function DashboardPage() {
  return (
    <>
      {/* Welcome */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          Goedemorgen, Coach 👋
        </h1>
        <p className="text-gray-400 text-sm mt-1">
          Hier is een overzicht van vandaag.
        </p>
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        {stats.map(({ label, value, icon: Icon, textColor, borderColor }) => (
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
            <div className="flex items-center gap-1 mt-4">
              <TrendingUp size={12} className="text-gray-300" />
              <span className="text-xs text-gray-300">Geen data beschikbaar</span>
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Upcoming lessons — empty state */}
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
                  Maak eerst je banen aan voordat je lessen plant.
                </p>
                <Link
                  href="/dashboard/courts/new"
                  className="inline-block mt-2 text-xs font-semibold text-tennis-green hover:underline"
                >
                  Baan aanmaken →
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
