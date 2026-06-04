import apiClient from "@/lib/api-client";

export type OnboardingStepKey = "mollie" | "club" | "trainerMode" | "series";

export interface OnboardingStepDto {
  key: OnboardingStepKey;
  completed: boolean;
}

export interface OnboardingStateDto {
  shouldShow: boolean;
  allCompleted: boolean;
  steps: OnboardingStepDto[];
  startedAt: string | null;
  dismissedAt: string | null;
}

export async function getOnboardingState(): Promise<OnboardingStateDto> {
  const { data } = await apiClient.get<OnboardingStateDto>("/onboarding/state");
  return data;
}

export async function dismissOnboarding(): Promise<void> {
  await apiClient.post("/onboarding/dismiss");
}

export async function setTrainerMode(
  adminActsAsTrainer: boolean,
): Promise<OnboardingStateDto> {
  const { data } = await apiClient.post<OnboardingStateDto>(
    "/onboarding/trainer-mode",
    { adminActsAsTrainer },
  );
  return data;
}
