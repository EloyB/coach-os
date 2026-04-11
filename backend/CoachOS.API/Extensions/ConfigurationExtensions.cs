using System.Text;
using CoachOS.Infrastructure.Persistence;
using LodeKennes.Extensions.Scaleway.SecretManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace CoachOS.API.Extensions;

public static class ConfigurationExtensions
{
    public static IConfiguration ConfigureAppConfiguration(
        this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddScalewayCliSecrets(options =>
            {
                options.ProjectId = Guid.Parse(
                    Environment.GetEnvironmentVariable("SCW_DEFAULT_PROJECT_ID")
                    ?? throw new InvalidOperationException("SCW_DEFAULT_PROJECT_ID niet gevonden."));
                options.EnableCaching(TimeSpan.FromMinutes(15));
                options.UseCredentials(
                    Environment.GetEnvironmentVariable("SCW_SECRET_KEY")
                        ?? throw new InvalidOperationException("SCW_SECRET_KEY niet gevonden."),
                    Environment.GetEnvironmentVariable("SCW_REGION") ?? "nl-ams",
                    Environment.GetEnvironmentVariable("SCW_DEFAULT_ORGANIZATION_ID")
                        ?? throw new InvalidOperationException("SCW_DEFAULT_ORGANIZATION_ID niet gevonden.")
                );
            });
        }

        return builder.Configuration;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
                     ?? throw new InvalidOperationException("Jwt:Key is niet geconfigureerd.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var frontendOrigin = isDevelopment
            ? "http://localhost:5317"
            : configuration["Frontend:Origin"] ?? "http://localhost:5317";

        services.AddCors(options =>
            options.AddPolicy("Frontend", policy =>
                policy.WithOrigins(frontendOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Voer je JWT token in. Voorbeeld: eyJhbGci..."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
