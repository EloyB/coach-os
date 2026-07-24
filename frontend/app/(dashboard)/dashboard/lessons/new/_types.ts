/** Format a Date as "YYYY-MM-DD" in local time (avoids UTC shift from toISOString). */
export function toLocalISODate(d: Date): string {
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export type WizardSlot = {
  id: string;
  dayOfWeek: number; // 0 = Monday, 6 = Sunday
  startTime: string; // "HH:mm"
  endTime: string; // "HH:mm"
  trainerId: string | null;
  trainerName: string | null;
  courtName: string | null;
  maxStudents: number;
  level: number | null;
};

export type WeekOverride = {
  weekIndex: number;
  weekStartDate: string; // ISO date of the Monday of that week
  slots: WizardSlot[];
};

export type Step1Data = {
  name: string;
  price: number;
  maxRegistrations: number;
  minAge: number;
  maxAge: number;
  tennisClubId: string;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
};

export type WizardData = Step1Data & {
  weeklyTemplate: WizardSlot[];
  weekOverrides: WeekOverride[];
};
