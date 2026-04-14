using CoachOS.Application.Auth;
using CoachOS.Application.Configuration;
using CoachOS.Application.StudentAuth;
using CoachOS.Application.Trainers;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
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

        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddSingleton<IMjmlTemplateRenderer, MjmlTemplateRenderer>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ILessonSerieRepository, LessonSerieRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ITennisClubRepository, TennisClubRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentFormRepository, EnrollmentFormRepository>();
        services.AddScoped<IEnrollmentGroupRepository, EnrollmentGroupRepository>();
        services.AddScoped<ITimeSlotPreferenceRepository, TimeSlotPreferenceRepository>();
        services.AddScoped<IScheduleAssignmentRepository, ScheduleAssignmentRepository>();
        services.AddScoped<IAssignmentConfirmationTokenRepository, AssignmentConfirmationTokenRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IMagicLinkTokenRepository, MagicLinkTokenRepository>();
        services.AddScoped<IStudentMagicLinkService, StudentMagicLinkService>();

        return services;
    }
}
