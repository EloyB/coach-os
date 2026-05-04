import {
  LayoutDashboard,
  CalendarDays,
  BookOpen,
  Users,
  GraduationCap,
  Settings,
  Ticket,
} from "lucide-react";

export const navItems = [
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
    href: "/dashboard/standalone-lessons",
    icon: Ticket,
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
  },
];
