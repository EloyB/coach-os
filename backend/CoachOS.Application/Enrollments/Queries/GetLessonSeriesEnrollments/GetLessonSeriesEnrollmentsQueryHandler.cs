using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.Enrollments.Queries.GetLessonSeriesEnrollments;

public class GetLessonSeriesEnrollmentsQueryHandler : IRequestHandler<GetLessonSeriesEnrollmentsQuery, Result<List<LessonSeriesEnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLessonSeriesEnrollmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LessonSeriesEnrollmentDto>>> Handle(GetLessonSeriesEnrollmentsQuery request, CancellationToken ct)
    {
        List<Enrollment> enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.FormResponses)
                .ThenInclude(r => r.FormField)
            .Where(e => e.LessonSeriesId == request.LessonSeriesId && e.OrganizationId == request.OrganizationId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

        List<LessonSeriesEnrollmentDto> dtos = enrollments.Select(e => new LessonSeriesEnrollmentDto
        {
            Id = e.Id,
            StudentName = e.StudentName,
            StudentEmail = e.StudentEmail,
            Status = e.Status.ToString(),
            EnrolledAt = e.EnrolledAt,
            Notes = e.Notes,
            FormResponses = e.FormResponses
                .OrderBy(r => r.FormField.Order)
                .Select(r => new EnrollmentResponseItemDto
                {
                    FieldLabel = r.FormField.Label,
                    Value = r.Value,
                }).ToList(),
        }).ToList();

        return Result<List<LessonSeriesEnrollmentDto>>.Success(dtos);
    }
}
