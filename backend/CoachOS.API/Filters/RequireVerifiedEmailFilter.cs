using CoachOS.API.Extensions;
using CoachOS.Application.Auth;

namespace CoachOS.API.Filters;

/// <summary>
/// Weigert acties die mails naar derden versturen (trainers, leerlingen) zolang de
/// gebruiker zijn eigen e-mailadres niet bevestigd heeft. Zo kan een vers, onbevestigd
/// account CoachOS niet gebruiken om willekeurige adressen te mailen.
/// </summary>
public class RequireVerifiedEmailFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        HttpContext httpContext = context.HttpContext;
        IAuthService authService = httpContext.RequestServices.GetRequiredService<IAuthService>();

        bool isVerified = await authService.IsEmailVerifiedAsync(httpContext.GetUserId(), httpContext.RequestAborted);
        if (isVerified)
            return await next(context);

        // 409 en geen 403: de gebruiker mág dit, alleen nog niet in de huidige staat van zijn
        // account — en de frontend toont 403's bewust niet als melding.
        return Results.Json(
            new[] { "Bevestig eerst je e-mailadres voor je mails naar trainers of leerlingen verstuurt." },
            statusCode: StatusCodes.Status409Conflict);
    }
}
