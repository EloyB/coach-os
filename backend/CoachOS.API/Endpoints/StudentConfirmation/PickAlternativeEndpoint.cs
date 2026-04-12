using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Application.StudentConfirmation.DTOs;

namespace CoachOS.API.Endpoints.StudentConfirmation;

public class PickAlternativeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/confirmation/{token}/pick-alternative",
            async (string token, PickAlternativeRequest request,
                IStudentConfirmationService service, CancellationToken ct) =>
            {
                var result = await service.PickAlternativeAsync(token, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }
}
