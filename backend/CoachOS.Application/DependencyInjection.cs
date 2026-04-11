using System.Reflection;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Enrollments;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Mappings;
using CoachOS.Application.Planning;
using CoachOS.Application.TennisClubs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ApplicationMapper>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILessonSerieService, LessonSerieService>();
        services.AddScoped<ITennisClubService, TennisClubService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IPlanningService, PlanningService>();

        return services;
    }
}
