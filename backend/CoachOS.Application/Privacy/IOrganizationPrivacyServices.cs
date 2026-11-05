namespace CoachOS.Application.Privacy;

public sealed record OrganizationErasureRequest(bool Apply = false, bool Early = false, Guid? RequestingAdminId = null, bool VerifiedRequest = false, string? Confirmation = null, string? PreviewHash = null);
public sealed record OrganizationErasureResult(bool Eligible, bool Deleted, string? PreviewHash, string[] Holds, int EnrollmentCount = 0, int CampEnrollmentCount = 0, int MembershipCount = 0);
public interface IOrganizationErasureService
{
    Task<OrganizationErasureResult> ProcessAsync(Guid organizationId, OrganizationErasureRequest request, CancellationToken ct = default, Guid? superAdminId = null);
}
public interface IOrganizationDataExport
{
    Task<byte[]?> ExportAsync(Guid organizationId, Guid actorId, CancellationToken ct = default);
}
