import { switchOrganization } from "@/lib/api/auth";
import { getAuthUser, setAuthUser, setToken } from "@/lib/auth";

/** Wisselt van organisatie en bewaart het nieuwe token en de gebruiker lokaal. */
export async function switchToOrganization(organizationId: string): Promise<void> {
  const response = await switchOrganization(organizationId);
  setToken(response.token);
  const current = getAuthUser();
  setAuthUser({
    userId: response.userId,
    email: response.email,
    firstName: response.firstName ?? current?.firstName,
    lastName: response.lastName ?? current?.lastName,
    organizationId: response.organizationId,
    role: response.role,
    memberships: response.memberships,
    headTrainerClubIds: response.headTrainerClubIds,
    emailVerified: response.emailVerified,
  });
}
