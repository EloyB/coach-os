using CoachOS.Application.Auth;
using CoachOS.Application.Configuration;
using CoachOS.Application.Export;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.StudentAuth;
using CoachOS.Application.SuperAdmin;
using CoachOS.Application.Trainers;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Export;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Mollie;
using CoachOS.Infrastructure.MultiTenancy;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Infrastructure;

/// <summary>
/// DI registratie voor de Infrastructure laag.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Scaleway Secret Manager pushes the connection string as
        // "DatabaseSettings__ConnectionString" (literal). Local dev uses
        // ConnectionStrings:DefaultConnection in appsettings.
        var connectionString = configuration["DatabaseSettings__ConnectionString"]
                               ?? configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "No DB connection string configured (DatabaseSettings__ConnectionString or ConnectionStrings:DefaultConnection).");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.Section));
        services.Configure<MollieOptions>(configuration.GetSection(MollieOptions.Section));
        services.Configure<SubscriptionOptions>(configuration.GetSection(SubscriptionOptions.SectionName));

        // TimeProvider is in .NET 8+ ingebouwd; testen kunnen FakeTimeProvider injecteren.
        services.AddSingleton(TimeProvider.System);

        // DataProtection sleutels persisteren naar disk zodat container-restarts
        // bestaande versleutelde Mollie tokens niet onleesbaar maken. Path is
        // configureerbaar via DataProtection:KeyDirectory; default is een vaste
        // map die in productie via een persistent volume gemount moet worden.
        string keyDirectory = configuration["DataProtection:KeyDirectory"]
                              ?? configuration["DataProtection__KeyDirectory"]
                              ?? Path.Combine(Path.GetTempPath(), "coachos-data-protection-keys");
        Directory.CreateDirectory(keyDirectory);
        services.AddDataProtection()
            .SetApplicationName("CoachOS")
            .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));

        services.AddHttpClient<IMollieClient, MollieClient>();
        services.AddScoped<ITokenProtector, DataProtectionTokenProtector>();
        services.AddScoped<IMollieConnectService, MollieConnectService>();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISuperAdminService, SuperAdminService>();
        services.AddHostedService<SuperAdminBootstrapHostedService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddSingleton<IMjmlTemplateRenderer, MjmlTemplateRenderer>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IPlanningWorkbookBuilder, ClosedXmlPlanningWorkbookBuilder>();
        services.AddScoped<ILessonSerieRepository, LessonSerieRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ITennisClubRepository, TennisClubRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentFormRepository, EnrollmentFormRepository>();
        services.AddScoped<IEnrollmentGroupRepository, EnrollmentGroupRepository>();
        services.AddScoped<ITimeSlotPreferenceRepository, TimeSlotPreferenceRepository>();
        services.AddScoped<ITrainerAvailabilityRepository, TrainerAvailabilityRepository>();
        services.AddScoped<IScheduleAssignmentRepository, ScheduleAssignmentRepository>();
        services.AddScoped<IAssignmentConfirmationTokenRepository, AssignmentConfirmationTokenRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IMagicLinkTokenRepository, MagicLinkTokenRepository>();
        services.AddScoped<IStudentMagicLinkService, StudentMagicLinkService>();
        services.AddScoped<IRescheduleRequestRepository, RescheduleRequestRepository>();
        services.AddScoped<ILessonInvitationRepository, LessonInvitationRepository>();
        services.AddScoped<IOrganizationSettingsRepository, OrganizationSettingsRepository>();
        services.AddScoped<IMollieConnectionRepository, MollieConnectionRepository>();
        services.AddScoped<IOAuthStateRepository, OAuthStateRepository>();
        services.AddScoped<ICampRepository, CampRepository>();
        services.AddScoped<ICampEnrollmentRepository, CampEnrollmentRepository>();
        services.AddScoped<ICampEnrollmentFormRepository, CampEnrollmentFormRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        return services;
    }
}
