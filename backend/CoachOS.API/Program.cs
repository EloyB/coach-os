using CoachOS.API.Endpoints;
using CoachOS.API.Extensions;
using CoachOS.API.Middleware;
using CoachOS.Application;
using CoachOS.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var isDevelopment = builder.Environment.IsDevelopment();
    var configuration = builder.ConfigureAppConfiguration();

    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .Enrich.FromLogContext()
              .WriteTo.Console());

    builder.Services.AddJwtAuthentication(configuration);
    builder.Services.AddCorsPolicy(configuration, isDevelopment);
    builder.Services.AddRateLimiting();
    builder.Services.AddSwagger();

    builder.Services.AddMemoryCache();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(configuration);
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    await app.ApplyMigrationsAsync();

    if (isDevelopment)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CoachOS API v1"));
    }

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var correlationId = context.Items[CorrelationIdMiddleware.ContextKey]?.ToString();

            if (exceptionFeature?.Error is UnauthorizedAccessException)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Niet geautoriseerd.",
                    correlationId,
                });
                return;
            }

            logger.LogError(exceptionFeature?.Error, "Onverwerkte fout bij {Path}", context.Request.Path);
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Er is een fout opgetreden. Probeer het later opnieuw.",
                correlationId,
            });
        });
    });

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<UserActiveValidationMiddleware>();
    app.UseMiddleware<TenantContextMiddleware>();
    app.UseMiddleware<OrganizationValidationMiddleware>();
    app.MapAllEndpoints();
    app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CoachOS API kon niet starten.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
