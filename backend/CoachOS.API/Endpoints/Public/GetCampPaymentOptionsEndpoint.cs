using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Public;

/// <summary>
/// Publieke betaalopties voor een kamp: prijs + of online betalen beschikbaar is. Met <c>campEnrollmentId</c>
/// is de prijs het te betalen totaal van die inschrijving (of groep) i.p.v. de hoofdprijs.
/// Gebruikt door de betaalpagina nadat een deelnemer zich heeft ingeschreven.
/// </summary>
public class GetCampPaymentOptionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/camps/{id:guid}/payment-options",
            async (Guid id, Guid? campEnrollmentId, ICampEnrollmentService service, CancellationToken ct) =>
            {
                Result<CampPaymentOptionsDto> result = await service.GetPaymentOptionsAsync(id, campEnrollmentId, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
