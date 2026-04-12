using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;

namespace CoachOS.API.Endpoints.StudentConfirmation;

public class GetAssignmentDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/confirmation/{token}",
            async (string token, IStudentConfirmationService service, CancellationToken ct) =>
            {
                var result = await service.GetByTokenAsync(token, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }
}
