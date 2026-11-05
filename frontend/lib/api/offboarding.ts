import apiClient from "@/lib/api-client";
export interface OrganizationErasureRequest {
  early: boolean;
  requestingAdminId: string;
  verifiedRequest: boolean;
  apply?: boolean;
  confirmation?: string;
  previewHash?: string;
}
export interface OrganizationErasureResult {
  eligible: boolean;
  deleted: boolean;
  previewHash: string | null;
  holds: string[];
  enrollmentCount: number;
  campEnrollmentCount: number;
  membershipCount: number;
}
export async function downloadOrganizationExport(): Promise<Blob> {
  const { data, headers } = await apiClient.get<Blob>(
    "/privacy/organization/export",
    { responseType: "blob" },
  );
  if (!String(headers["content-type"]).includes("application/zip"))
    throw new Error("unexpected_export_content_type");
  return data;
}
export async function processOrganizationErasure(
  id: string,
  request: OrganizationErasureRequest,
) {
  return (
    await apiClient.post<OrganizationErasureResult>(
      `/super-admin/organizations/${encodeURIComponent(id)}/erasure`,
      request,
    )
  ).data;
}
