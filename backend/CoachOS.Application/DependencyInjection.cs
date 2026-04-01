using System.Reflection;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Enrollments;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.Mappings;
using CoachOS.Application.TennisClubs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton<ApplicationMapper>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILessonSeriesService, LessonSeriesService>();
        services.AddScoped<ITennisClubService, TennisClubService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        return services;
    }
}
