"use client";

import type { AuthUser } from "@/lib/auth";
import { ActiveStudentsCard } from "./active-students-card";

export function ActiveStudentsSidebarCard({ user }: { user: AuthUser | null }) {
  return <ActiveStudentsCard user={user} variant="sidebar" />;
}
