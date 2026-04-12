using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;

namespace CoachOS.API.Endpoints.StudentConfirmation;

public class GetAvailableSlotsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/confirmation/{token}/available-slots",
            async (string token, IStudentConfirmationService service, CancellationToken ct) =>
            {
                var result = await service.GetAvailableSlotsAsync(token, ct);
                return result.IsSuccess ? Results.Ok(new { slots = result.Value }) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }
}
