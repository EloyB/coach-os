namespace CoachOS.API.Endpoints;

public static class EndpointMappingExtensions
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        IEndpointRouteBuilder builder = app.MapGroup("/api");

        var isDevelopment = app.Environment.IsDevelopment();

        var endpointTypes = typeof(EndpointMappingExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            // Dev-only endpoints (Endpoints/Dev/*) zijn enkel beschikbaar in Development.
            // Hard guard tegen accidentele exposure in productie.
            if (!isDevelopment && (type.Namespace?.Contains(".Endpoints.Dev") ?? false))
                continue;

            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(builder);
        }
    }
}
