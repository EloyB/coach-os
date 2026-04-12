using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;

namespace CoachOS.API.Endpoints.StudentConfirmation;

public class DeclineAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/confirmation/{token}/decline",
            async (string token, IStudentConfirmationService service, CancellationToken ct) =>
            {
                var result = await service.DeclineAsync(token, ct);
                return result.IsSuccess ? Results.Ok(new { availableSlots = result.Value }) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }
}
