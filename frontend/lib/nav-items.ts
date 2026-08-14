import {
  LayoutDashboard,
  CalendarDays,
  BookOpen,
  GraduationCap,
  Settings,
  Ticket,
  Tent,
} from "lucide-react";

export const navItems = [
  {
    label: "Vandaag",
    href: "/dashboard",
    icon: LayoutDashboard,
    exact: true,
  },
  {
    label: "Planning",
    href: "/dashboard/planning",
    icon: CalendarDays,
    exact: true,
  },
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
