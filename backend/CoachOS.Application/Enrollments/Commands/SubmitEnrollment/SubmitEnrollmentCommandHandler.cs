using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Enrollments.Commands.SubmitEnrollment;

public class SubmitEnrollmentCommandHandler : IRequestHandler<SubmitEnrollmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookup;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubmitEnrollmentCommandHandler> _logger;

    public SubmitEnrollmentCommandHandler(
        IApplicationDbContext context,
        IUserLookupService userLookup,
        IEmailService emailService,
        ILogger<SubmitEnrollmentCommandHandler> logger)
    {
        _context = context;
        _userLookup = userLookup;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SubmitEnrollmentCommand request, CancellationToken ct)
    {
        // 1. Load the lesson series
        Domain.Entities.LessonSeries? series = await _context.LessonSeries
            .AsNoTracking()
            .FirstOrDefaultAsync(ls => ls.Id == request.LessonSeriesId && ls.IsActive, ct);

        if (series is null)
            return Result<Guid>.Failure("Lessenreeks niet gevonden");

        // 2. Load enrollment form with fields (may be null)
        EnrollmentForm? form = await _context.EnrollmentForms
            .AsNoTracking()
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.LessonSeriesId == request.LessonSeriesId, ct);

        // 3. Validate required custom fields
        if (form is not null)
        {
            List<Domain.Entities.FormField> requiredFields = form.Fields
                .Where(f => f.IsRequired)
                .ToList();

            foreach (Domain.Entities.FormField requiredField in requiredFields)
            {
                bool hasResponse = request.Responses.Any(r =>
                    r.FormFieldId == requiredField.Id && !string.IsNullOrWhiteSpace(r.Value));

                if (!hasResponse)
                    return Result<Guid>.Failure($"Veld '{requiredField.Label}' is verplicht");
            }
        }

        // 4. Duplicate check: same email + series + Confirmed/Pending
        bool isDuplicate = await _context.Enrollments.AnyAsync(e =>
            e.LessonSeriesId == request.LessonSeriesId &&
            e.StudentEmail == request.StudentEmail &&
            (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending), ct);

        if (isDuplicate)
            return Result<Guid>.Failure("Je bent al ingeschreven voor deze lessenreeks");

        // 5. Create enrollment
        Enrollment enrollment = new()
        {
            OrganizationId = series.OrganizationId,
            LessonSeriesId = series.Id,
            StudentName = request.StudentName,
            StudentEmail = request.StudentEmail,
            Status = EnrollmentStatus.Confirmed,
            EnrolledAt = DateTime.UtcNow,
        };

        _context.Enrollments.Add(enrollment);

        // 6. Create form responses
        foreach (Enrollments.DTOs.FormResponseValueDto responseDto in request.Responses)
        {
            FormResponse response = new()
            {
                EnrollmentId = enrollment.Id,
                FormFieldId = responseDto.FormFieldId,
                Value = responseDto.Value,
            };
            _context.FormResponses.Add(response);
        }

        await _context.SaveChangesAsync(ct);

        // 7. Send notification emails (fire-and-forget in try/catch)
        try
        {
            (string FullName, string Email)? trainerInfo =
                await _userLookup.GetUserInfoByIdAsync(series.TrainerId, ct);

            List<(string FieldLabel, string Value)> responseItems = new();
            if (form is not null)
            {
                foreach (Enrollments.DTOs.FormResponseValueDto r in request.Responses)
                {
                    Domain.Entities.FormField? field = form.Fields.FirstOrDefault(f => f.Id == r.FormFieldId);
                    if (field is not null)
                        responseItems.Add((field.Label, r.Value));
                }
            }

            await _emailService.SendEnrollmentConfirmationAsync(
                request.StudentEmail,
                request.StudentName,
                series.Name,
                trainerInfo?.FullName ?? string.Empty,
                ct);

            if (trainerInfo.HasValue)
            {
                await _emailService.SendEnrollmentNotificationToTrainerAsync(
                    trainerInfo.Value.Email,
                    trainerInfo.Value.FullName,
                    request.StudentName,
                    request.StudentEmail,
                    series.Name,
                    responseItems,
                    ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-mailnotificatie mislukt voor inschrijving {EnrollmentId}", enrollment.Id);
        }

        return Result<Guid>.Success(enrollment.Id);
    }
}
