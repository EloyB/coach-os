using System.Text;
using CoachOS.API.Auth;
using System.Threading.RateLimiting;
using CoachOS.Infrastructure.Persistence;
using LodeKennes.Extensions.Scaleway.SecretManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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

            // Scaleway Secret Manager stores keys with their literal names
            // (e.g. "Email__SmtpHost"). .NET config section-binding expects
            // colon form ("Email:SmtpHost"). Duplicate every "__"-key as
            // ":"-key so GetSection<T> / Bind<T> / configuration[...] all work
            // uniformly regardless of which form the caller uses.
            var normalized = new Dictionary<string, string?>();
            foreach (var kv in builder.Configuration.AsEnumerable())
            {
                if (kv.Key.Contains("__") && !string.IsNullOrEmpty(kv.Value))
                {
                    normalized[kv.Key.Replace("__", ":")] = kv.Value;
                }
            }
            if (normalized.Count > 0)
            {
                builder.Configuration.AddInMemoryCollection(normalized);
            }
        }

        return builder.Configuration;
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddJwtAuthentication(IConfiguration configuration)
        {
            // Scaleway Secret Manager pushes secrets with their literal names
            // ("Jwt__Key"), not the .NET colon-separated form. Dev uses nested
            // JSON (reads as "Jwt:Key"). Prefer the Scaleway form, fall back
            // to the colon form for local dev.
            var jwtKey = configuration["Jwt__Key"] ?? configuration["Jwt:Key"]
                         ?? throw new InvalidOperationException("Jwt:Key is niet geconfigureerd.");

            // Reject the placeholder from appsettings.json and any key shorter than the
            // HMAC-SHA256 minimum (256 bits). Prevents shipping with a dev key in prod.
            if (jwtKey.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Jwt:Key is nog op de placeholder-waarde. Stel een productiesleutel in via de omgeving of Scaleway Secret Manager.");
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
                throw new InvalidOperationException(
                    "Jwt:Key moet minimaal 32 bytes (256 bits) zijn voor HMAC-SHA256.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt__Issuer"] ?? configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt__Audience"] ?? configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            services.AddAuthorization(options =>
            {
                // System-level super admin: enkel users met de IsSuperAdmin claim.
                // Tokens met deze policy bevatten GEEN organizationId — super admins
                // opereren los van organisaties (zie issue #91, Option C).
                options.AddPolicy(AuthorizationPolicies.SuperAdmin, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim(CoachOsClaims.IsSuperAdmin, "true"));

                // Read-only inschrijvingen + planning: Admin, of een hoofdtrainer
                // (Trainer met ≥1 headTrainerClub-claim). De fijne per-reeks club-check
                // gebeurt in HeadTrainerAccess in de endpoints.
                options.AddPolicy(AuthorizationPolicies.EnrollmentsPlanningRead, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx =>
                              ctx.User.IsInRole("Admin") ||
                              ctx.User.HasClaim(c => c.Type == CoachOsClaims.HeadTrainerClub)));
            });

            return services;
        }

        public IServiceCollection AddCorsPolicy(IConfiguration configuration,
            bool isDevelopment)
        {
            string frontendOrigin;
            if (isDevelopment)
            {
                frontendOrigin = "http://localhost:5317";
            }
            else
            {
                // In production, Frontend:Origin MUST be configured. Silently defaulting to
                // localhost would make the API unusable from any real client.
                frontendOrigin = configuration["Frontend__Origin"] ?? configuration["Frontend:Origin"]
                    ?? throw new InvalidOperationException(
                        "Frontend:Origin is verplicht in productie (bv. https://coach-os.be).");
            }

            services.AddCors(options =>
                options.AddPolicy("Frontend", policy =>
                    policy.WithOrigins(frontendOrigin)
                        .WithHeaders("Content-Type", "Authorization", "Accept")
                        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                        .AllowCredentials()));

            return services;
        }

        public IServiceCollection AddRateLimiting()
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter("auth", limiter =>
                {
                    limiter.PermitLimit = 5;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("public", limiter =>
                {
                    limiter.PermitLimit = 30;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("authenticated", limiter =>
                {
                    limiter.PermitLimit = 60;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                });

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 300,
                            Window = TimeSpan.FromMinutes(1),
                        }));
            });

            return services;
        }

        public IServiceCollection AddSwagger()
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
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
