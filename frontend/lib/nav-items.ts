import { LayoutDashboard, Map, BookOpen, GraduationCap, Users, Settings } from "lucide-react";

export const navItems = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard, exact: true },
  { label: "Banen", href: "/dashboard/courts", icon: Map, exact: false },
  { label: "Lessen", href: "/dashboard/lessons", icon: BookOpen, exact: false },
  { label: "Trainers", href: "/dashboard/trainers", icon: GraduationCap, exact: false, adminOnly: true },
  { label: "Leerlingen", href: "/dashboard/students", icon: Users, exact: false },
  { label: "Instellingen", href: "/dashboard/settings", icon: Settings, exact: false },
];
