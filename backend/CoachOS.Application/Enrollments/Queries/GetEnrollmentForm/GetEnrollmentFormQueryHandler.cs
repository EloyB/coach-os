using System.Text.Json;
using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.Enrollments.Queries.GetEnrollmentForm;

public class GetEnrollmentFormQueryHandler : IRequestHandler<GetEnrollmentFormQuery, Result<EnrollmentFormDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentFormQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<EnrollmentFormDto?>> Handle(GetEnrollmentFormQuery request, CancellationToken ct)
    {
        EnrollmentForm? form = await _context.EnrollmentForms
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(ff => ff.Order))
            .FirstOrDefaultAsync(f => f.LessonSeriesId == request.LessonSeriesId, ct);

        if (form is null)
            return Result<EnrollmentFormDto?>.Success(null);

        EnrollmentFormDto dto = new()
        {
            Id = form.Id,
            LessonSeriesId = form.LessonSeriesId,
            Fields = form.Fields.Select(f => new FormFieldDto
            {
                Id = f.Id,
                Label = f.Label,
                Type = (int)f.Type,
                IsRequired = f.IsRequired,
                Order = f.Order,
                Options = f.Options is not null
                    ? JsonSerializer.Deserialize<List<string>>(f.Options)
                    : null,
            }).ToList(),
        };

        return Result<EnrollmentFormDto?>.Success(dto);
    }
}
