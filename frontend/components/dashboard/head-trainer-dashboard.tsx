"use client";

import { TrainerDashboard } from "@/components/dashboard/trainer-dashboard";

/**
 * Dashboard voor een hoofdtrainer (rol Trainer met minstens één hoofdtrainer-club).
 *
 * Start bewust identiek aan het trainersdashboard; de club-specifieke onderdelen
 * (clublessen, aandachtspunten en cijfers per club) komen hier stap voor stap bij.
 */
export function HeadTrainerDashboard() {
  return <TrainerDashboard variant="head-trainer" />;
}
