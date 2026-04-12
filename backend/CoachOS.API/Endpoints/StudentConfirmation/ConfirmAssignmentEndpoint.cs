using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Application.StudentConfirmation.DTOs;

namespace CoachOS.API.Endpoints.StudentConfirmation;

public class ConfirmAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/confirmation/{token}/confirm",
            async (string token, ConfirmRequest request,
                IStudentConfirmationService service, CancellationToken ct) =>
            {
                var result = await service.ConfirmAsync(token, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }
}
