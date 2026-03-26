using System.Text.Json;
using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.Enrollments.Commands.SaveEnrollmentForm;

public class SaveEnrollmentFormCommandHandler : IRequestHandler<SaveEnrollmentFormCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public SaveEnrollmentFormCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(SaveEnrollmentFormCommand request, CancellationToken ct)
    {
        // Verify the lesson series belongs to the organization
        bool seriesExists = await _context.LessonSeries
            .AnyAsync(ls => ls.Id == request.LessonSeriesId && ls.OrganizationId == request.OrganizationId, ct);

        if (!seriesExists)
            return Result<Guid>.Failure("Lessenreeks niet gevonden");

        // Load existing form with fields
        EnrollmentForm? form = await _context.EnrollmentForms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.LessonSeriesId == request.LessonSeriesId, ct);

        if (form is null)
        {
            form = new EnrollmentForm
            {
                OrganizationId = request.OrganizationId,
                LessonSeriesId = request.LessonSeriesId,
            };
            _context.EnrollmentForms.Add(form);
        }

        // Determine which existing fields to delete
        List<Guid> incomingIds = request.Fields
            .Where(f => f.Id.HasValue)
            .Select(f => f.Id!.Value)
            .ToList();

        List<FormField> toDelete = form.Fields
            .Where(f => !incomingIds.Contains(f.Id))
            .ToList();

        foreach (FormField field in toDelete)
            _context.FormFields.Remove(field);

        // Update existing + insert new
        int order = 0;
        foreach (SaveFormFieldDto dto in request.Fields)
        {
            string? optionsJson = dto.Type == (int)FormFieldType.MultipleChoice && dto.Options?.Count > 0
                ? JsonSerializer.Serialize(dto.Options)
                : null;

            if (dto.Id.HasValue)
            {
                FormField? existing = form.Fields.FirstOrDefault(f => f.Id == dto.Id.Value);
                if (existing is not null)
                {
                    existing.Label = dto.Label;
                    existing.Type = (FormFieldType)dto.Type;
                    existing.IsRequired = dto.IsRequired;
                    existing.Order = order;
                    existing.Options = optionsJson;
                }
            }
            else
            {
                FormField newField = new()
                {
                    EnrollmentFormId = form.Id,
                    Label = dto.Label,
                    Type = (FormFieldType)dto.Type,
                    IsRequired = dto.IsRequired,
                    Order = order,
                    Options = optionsJson,
                };
                form.Fields.Add(newField);
            }

            order++;
        }

        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(form.Id);
    }
}
