namespace CoachOS.API.Endpoints;

public static class EndpointMappingExtensions
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        IEndpointRouteBuilder builder = app.MapGroup("/api");

        var endpointTypes = typeof(EndpointMappingExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (Type type in endpointTypes)
        {
            IEndpoint endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(builder);
        }
    }
}
