import Link from "next/link";
import {
  LayoutDashboard,
  Map,
  BookOpen,
  Users,
  Settings,
  LogOut,
  Plus,
  Calendar,
  CreditCard,
  TrendingUp,
} from "lucide-react";

// ─── Sidebar ────────────────────────────────────────────────────────────────

function CourtLines() {
  return (
    <svg
      className="absolute inset-0 w-full h-full opacity-[0.07]"
      viewBox="0 0 240 800"
      fill="none"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
    >
      <rect
        x="20"
        y="60"
        width="200"
        height="680"
        stroke="white"
        strokeWidth="1.5"
      />
      <line x1="20" y1="400" x2="220" y2="400" stroke="white" strokeWidth="2" />
      <line x1="20" y1="222" x2="220" y2="222" stroke="white" strokeWidth="1" />
      <line x1="20" y1="578" x2="220" y2="578" stroke="white" strokeWidth="1" />
      <line
        x1="120"
        y1="222"
        x2="120"
        y2="578"
        stroke="white"
        strokeWidth="1"
      />
      <line x1="115" y1="60" x2="125" y2="60" stroke="white" strokeWidth="2" />
      <line
        x1="115"
        y1="740"
        x2="125"
        y2="740"
        stroke="white"
        strokeWidth="2"
      />
    </svg>
  );
}

const navItems = [
  {
    label: "Dashboard",
    href: "/dashboard",
    icon: LayoutDashboard,
    active: true,
  },
  { label: "Banen", href: "/dashboard/courts", icon: Map, active: false },
  {
    label: "Lessen",
    href: "/dashboard/lessons",
    icon: BookOpen,
    active: false,
  },
  {
    label: "Leerlingen",
    href: "/dashboard/students",
    icon: Users,
    active: false,
  },
  {
    label: "Instellingen",
    href: "/dashboard/settings",
    icon: Settings,
    active: false,
  },
];

function Sidebar() {
  return (
    <aside className="hidden lg:flex flex-col w-55 bg-tennis-green relative overflow-hidden shrink-0">
      <CourtLines />

      {/* Logo */}
      <div className="relative z-10 px-6 py-7 border-b border-white/10">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-full bg-tennis-lime flex items-center justify-center shrink-0">
            <svg
              viewBox="0 0 24 24"
              fill="none"
              className="w-4.5 h-4.5"
              aria-hidden="true"
            >
              <circle
                cx="12"
                cy="12"
                r="9"
                stroke="#2D5016"
                strokeWidth="2.5"
              />
              <path
                d="M3.5 8.5Q12 6 20.5 8.5"
                stroke="#2D5016"
                strokeWidth="1.5"
                strokeLinecap="round"
              />
              <path
                d="M3.5 15.5Q12 18 20.5 15.5"
                stroke="#2D5016"
                strokeWidth="1.5"
                strokeLinecap="round"
              />
            </svg>
          </div>
          <span className="text-white font-bold text-lg tracking-tight leading-none">
            Coach<span className="text-tennis-lime">OS</span>
          </span>
        </div>
      </div>

      {/* Nav */}
      <nav className="relative z-10 flex-1 px-3 py-5 space-y-0.5">
        {navItems.map(({ label, href, icon: Icon, active }) => (
          <Link
            key={href}
            href={href}
            className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors group ${
              active
                ? "bg-white/15 text-white"
                : "text-white/60 hover:text-white hover:bg-white/10"
            }`}
          >
            {active && (
              <span
                className="absolute left-3 w-0.5 h-5 bg-tennis-lime rounded-full"
                aria-hidden="true"
              />
            )}
            <Icon
              size={16}
              className={`shrink-0 transition-colors ${
                active
                  ? "text-tennis-lime"
                  : "text-white/50 group-hover:text-white/80"
              }`}
            />
            <span>{label}</span>
          </Link>
        ))}
      </nav>

      {/* Logout */}
      <div className="relative z-10 px-3 py-5 border-t border-white/10">
        <Link
          href="/login"
          className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-white/50 hover:text-white hover:bg-white/10 transition-colors group"
        >
          <LogOut size={16} className="shrink-0 group-hover:text-white/80" />
          <span>Uitloggen</span>
        </Link>
      </div>
    </aside>
  );
}

// ─── Stat Cards ──────────────────────────────────────────────────────────────

const stats = [
  {
    label: "Actieve banen",
    value: "0",
    icon: Map,
    color: "bg-tennis-green",
    textColor: "text-tennis-green",
    borderColor: "border-l-tennis-green",
  },
  {
    label: "Lessen deze week",
    value: "0",
    icon: Calendar,
    color: "bg-blue-500",
    textColor: "text-blue-600",
    borderColor: "border-l-blue-500",
  },
  {
    label: "Leerlingen",
    value: "0",
    icon: Users,
    color: "bg-violet-500",
    textColor: "text-violet-600",
    borderColor: "border-l-violet-500",
  },
  {
    label: "Openstaande betalingen",
    value: "€0",
    icon: CreditCard,
    color: "bg-amber-500",
    textColor: "text-amber-600",
    borderColor: "border-l-amber-500",
  },
];

// ─── Quick Actions ────────────────────────────────────────────────────────────

const quickActions = [
  { label: "Nieuwe les", href: "/dashboard/lessons/new", icon: Plus },
  { label: "Nieuwe baan", href: "/dashboard/courts/new", icon: Map },
  { label: "Leerling toevoegen", href: "/dashboard/students/new", icon: Users },
];

// ─── Page ────────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  return (
    <div className="flex h-screen bg-[#F5F4F1] overflow-hidden">
      <Sidebar />

      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Topbar */}
        <header className="bg-white border-b border-gray-100 px-8 py-4 flex items-center justify-between shrink-0">
          <div>
            <p className="text-xs text-gray-400 font-medium uppercase tracking-wider">
              Tennis &amp; Padel Platform
            </p>
          </div>
          <div className="flex items-center gap-3">
            <div className="text-right">
              <p className="text-sm font-semibold text-gray-800 leading-none">
                Coach
              </p>
              <p className="text-xs text-gray-400 mt-0.5">Beheerder</p>
            </div>
            <div className="w-9 h-9 rounded-full bg-tennis-green flex items-center justify-center shrink-0">
              <span className="text-tennis-lime text-sm font-bold">C</span>
            </div>
          </div>
        </header>

        {/* Main scroll area */}
        <main className="flex-1 overflow-y-auto px-8 py-8">
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
            {stats.map(
              ({ label, value, icon: Icon, textColor, borderColor }) => (
                <div
                  key={label}
                  className={`bg-white rounded-xl p-5 border-l-4 ${borderColor} shadow-sm shadow-gray-100`}
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="text-xs text-gray-400 font-medium mb-2 uppercase tracking-wide">
                        {label}
                      </p>
                      <p
                        className={`text-3xl font-bold ${textColor} leading-none`}
                      >
                        {value}
                      </p>
                    </div>
                    <div className={`p-2 rounded-lg bg-gray-50`}>
                      <Icon size={16} className="text-gray-400" />
                    </div>
                  </div>
                  <div className="flex items-center gap-1 mt-4">
                    <TrendingUp size={12} className="text-gray-300" />
                    <span className="text-xs text-gray-300">
                      Geen data beschikbaar
                    </span>
                  </div>
                </div>
              ),
            )}
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
                  <svg
                    viewBox="0 0 48 48"
                    fill="none"
                    className="w-8 h-8"
                    aria-hidden="true"
                  >
                    <circle
                      cx="24"
                      cy="24"
                      r="18"
                      stroke="#2D5016"
                      strokeWidth="2.5"
                      strokeOpacity="0.4"
                    />
                    <path
                      d="M7 18Q24 13 41 18"
                      stroke="#2D5016"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeOpacity="0.4"
                    />
                    <path
                      d="M7 30Q24 35 41 30"
                      stroke="#2D5016"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeOpacity="0.4"
                    />
                    <line
                      x1="24"
                      y1="6"
                      x2="24"
                      y2="42"
                      stroke="#2D5016"
                      strokeWidth="2"
                      strokeOpacity="0.4"
                    />
                  </svg>
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

                {/* Divider + tip */}
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
        </main>
      </div>
    </div>
  );
}
