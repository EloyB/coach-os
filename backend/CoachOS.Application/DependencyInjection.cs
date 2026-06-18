using System.Reflection;
using CoachOS.Application.Camps;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Export;
using CoachOS.Application.LessonReschedule;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Mappings;
using CoachOS.Application.Onboarding;
using CoachOS.Application.OrganizationSettings;
using CoachOS.Application.Payments;
using CoachOS.Application.Planning;
using CoachOS.Application.Reschedule;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Application.Students;
using CoachOS.Application.TennisClubs;
using CoachOS.Application.TrainerAvailabilities;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ApplicationMapper>();

        services.AddScoped<ICampService, CampService>();
        services.AddScoped<ICampEnrollmentService, CampEnrollmentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILessonSerieService, LessonSerieService>();
        services.AddScoped<ITennisClubService, TennisClubService>();
        services.AddScoped<ITrainerAvailabilityService, TrainerAvailabilityService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IConfirmationOrchestrationService, ConfirmationOrchestrationService>();
        services.AddScoped<IStudentConfirmationService, StudentConfirmationService>();
        services.AddScoped<IStudentLessonsService, StudentLessonsService>();
        services.AddScoped<IRescheduleService, RescheduleService>();
        services.AddScoped<ILessonRescheduleService, LessonRescheduleService>();
        services.AddScoped<IStandaloneLessonService, StandaloneLessonService>();
        services.AddScoped<IInvitationPublicService, InvitationPublicService>();
        services.AddScoped<IOrganizationSettingsService, OrganizationSettingsService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPlanningExportService, PlanningExportService>();

        return services;
    }
}
