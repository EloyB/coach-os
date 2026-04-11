using CoachOS.API.Endpoints;
using CoachOS.API.Extensions;
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
              .WriteTo.Console());

    builder.Services.AddJwtAuthentication(configuration);
    builder.Services.AddCorsPolicy(configuration, isDevelopment);
    builder.Services.AddSwagger();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(configuration);

    var app = builder.Build();

    await app.ApplyMigrationsAsync();

    if (isDevelopment)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CoachOS API v1"));
    }

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapAllEndpoints();

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
