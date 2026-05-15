import apiClient from "@/lib/api-client";

export interface OrganizationSettingsDto {
  adminsActAsTrainers: boolean;
}

export interface UpdateOrganizationSettingsRequest {
  adminsActAsTrainers: boolean;
}

export async function getOrganizationSettings(): Promise<OrganizationSettingsDto> {
  const { data } = await apiClient.get<OrganizationSettingsDto>(
    "/organization/settings",
  );
  return data;
}

export async function updateOrganizationSettings(
  request: UpdateOrganizationSettingsRequest,
): Promise<OrganizationSettingsDto> {
  const { data } = await apiClient.put<OrganizationSettingsDto>(
    "/organization/settings",
    request,
  );
  return data;
}
