import {
  LayoutDashboard,
  CalendarDays,
  BookOpen,
  Users,
  GraduationCap,
  Settings,
  Ticket,
  Tent,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  label: string;
  /** Kortere variant voor de mobiele bottom-nav (valt terug op label). */
  shortLabel?: string;
  href: string;
  icon: LucideIcon;
  exact?: boolean;
  adminOnly?: boolean;
}

export const navItems: NavItem[] = [
  {
    label: "Vandaag",
    href: "/dashboard",
    icon: LayoutDashboard,
    exact: true,
  },
  // {
  //   label: "Planning",
  //   href: "/dashboard/planning",
  //   icon: CalendarDays,
  //   exact: false,
  // },
  {
    label: "Lesreeksen",
    href: "/dashboard/lessons",
    icon: BookOpen,
    exact: false,
  },
  {
    label: "Losse lessen",
    shortLabel: "Losse",
    href: "/dashboard/standalone-lessons",
    icon: Ticket,
    exact: false,
  },
  {
    label: "Kampen",
    href: "/dashboard/camps",
    icon: Tent,
    exact: false,
  },
  // {
  //   label: "Leerlingen",
  //   href: "/dashboard/students",
  //   icon: Users,
  //   exact: false,
  // },
  {
    label: "Trainers",
    href: "/dashboard/trainers",
    icon: GraduationCap,
    exact: false,
    adminOnly: true,
  },
  {
    label: "Instellingen",
    href: "/dashboard/settings",
    icon: Settings,
    exact: false,
    adminOnly: true,
  },
];
