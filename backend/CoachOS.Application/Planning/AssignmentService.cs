using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Planning;

public class AssignmentService(
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo,
    IEmailService emailService,
    ILogger<AssignmentService> logger) : IAssignmentService
{
    public async Task<Result<Guid>> CreateAssignmentAsync(
        Guid seriesId, CreateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == request.WeeklyTemplateEntryId);
        if (slot is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Tijdslot niet gevonden."));

        var existingAssignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);

        int addSize;
        Guid? enrollmentId = null;
        Guid? groupId = null;

        // Een eerder geweigerde (Declined) toewijzing op ditzelfde slot wordt niet
        // geblokkeerd maar heractiveerd (foutieve afwijzing / bewuste heroverweging).
        // Een actieve duplicaat blijft wél geweigerd (spiegelt de DB-index).
        Guid? reactivateId = null;

        if (request.GroupId.HasValue)
        {
            var group = await enrollmentGroupRepo.GetByIdAsync(request.GroupId.Value, organizationId, ct);
            if (group is null || group.LessonSerieId != seriesId)
                return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

            var groupDup = existingAssignments.FirstOrDefault(a =>
                a.EnrollmentGroupId == request.GroupId
                && a.WeeklyTemplateEntryId == request.WeeklyTemplateEntryId);
            if (groupDup is not null)
            {
                if (groupDup.Status != ScheduleAssignmentStatus.Declined)
                    return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Groep staat al op dit tijdslot."));
                reactivateId = groupDup.Id;
            }

            groupId = group.Id;
            addSize = PlanningProposalBuilder.GetEffectiveAssignmentSize(new ScheduleAssignment
            {
                EnrollmentGroup = group,
                Status = ScheduleAssignmentStatus.Proposed,
            });
        }
        else
        {
            var enrollment = await enrollmentRepo.GetByIdAsync(request.EnrollmentId!.Value, organizationId, ct);
            if (enrollment is null || enrollment.LessonSerieId != seriesId)
                return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

            var dup = existingAssignments.FirstOrDefault(a =>
                a.EnrollmentId == request.EnrollmentId
                && a.WeeklyTemplateEntryId == request.WeeklyTemplateEntryId);
            if (dup is not null)
            {
                if (dup.Status != ScheduleAssignmentStatus.Declined)
                    return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Inschrijving staat al op dit tijdslot."));
                reactivateId = dup.Id;
            }

            enrollmentId = enrollment.Id;
            addSize = 1;
        }

        // De geweigerde toewijzing telt nog 0 mee (size 0), dus de capaciteit klopt
        // ook wanneer we ze straks heractiveren.
        var capacityError = await EnsureSlotCapacityAsync(
            seriesId, organizationId, request.WeeklyTemplateEntryId, addSize, excludeAssignmentId: null, ct);
        if (capacityError is not null)
            return Result<Guid>.Fail(capacityError);

        // Heractiveer de geweigerde toewijzing (hergebruik de rij i.p.v. een duplicaat
        // te maken die de unieke index zou schenden).
        if (reactivateId is not null)
        {
            var toReactivate = await scheduleAssignmentRepo.GetByIdAsync(reactivateId.Value, organizationId, ct);
            if (toReactivate is null)
                return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

            toReactivate.Status = ScheduleAssignmentStatus.Proposed;
            toReactivate.IsAutoMerged = false;
            toReactivate.IsLocked = true;
            await scheduleAssignmentRepo.SaveChangesAsync(ct);
            return Result<Guid>.Ok(toReactivate.Id);
        }

        ScheduleAssignment assignment = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            WeeklyTemplateEntryId = request.WeeklyTemplateEntryId,
            EnrollmentId = enrollmentId,
            EnrollmentGroupId = groupId,
            Status = ScheduleAssignmentStatus.Proposed,
            IsAutoMerged = false,
            IsLocked = true,
        };

        await scheduleAssignmentRepo.AddRangeAsync([assignment], ct);
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(assignment.Id);
    }

    public async Task<Result<bool>> UpdateAssignmentAsync(
        Guid seriesId, Guid assignmentId, UpdateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        // Een bevestigde toewijzing mag de admin nog naar een ander tijdslot verplaatsen: de
        // betaling hangt aan de inschrijving (niet aan het slot) en blijft dus geldig. Verwijderen
        // van een bevestigde toewijzing blijft wél geblokkeerd (zie DeleteAssignmentAsync).
        var capacityError = await EnsureSlotCapacityAsync(
            seriesId, organizationId, request.WeeklyTemplateEntryId,
            addSize: PlanningProposalBuilder.GetEffectiveAssignmentSize(assignment),
            excludeAssignmentId: assignment.Id, ct);
        if (capacityError is not null)
            return Result<bool>.Fail(capacityError);

        // Oud slot vastleggen vóór de wijziging (nodig voor de "verplaatst"-mail).
        var wasConfirmed = assignment.Status == ScheduleAssignmentStatus.Confirmed;
        var oldEntry = assignment.WeeklyTemplateEntry;

        assignment.WeeklyTemplateEntryId = request.WeeklyTemplateEntryId;
        assignment.IsLocked = true;
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        // Enkel bij een bevestigde verplaatsing én expliciete keuze de lesnemer mailen.
        // De verplaatsing is al gecommit: een mislukte mail loggen we, maar draaien we
        // niet terug (de admin kan altijd handmatig contact opnemen).
        if (wasConfirmed && request.NotifyStudent && oldEntry is not null)
            await NotifyAssignmentMovedAsync(
                seriesId, organizationId, assignment, oldEntry, request.WeeklyTemplateEntryId, ct);

        return Result<bool>.Ok(true);
    }

    private async Task NotifyAssignmentMovedAsync(
        Guid seriesId, Guid organizationId, ScheduleAssignment assignment,
        WeeklyTemplateEntry oldEntry, Guid newEntryId, CancellationToken ct)
    {
        try
        {
            var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
            var newEntry = series?.WeeklyTemplate.FirstOrDefault(s => s.Id == newEntryId);
            if (series is null || newEntry is null) return;

            string toEmail, toName;
            IReadOnlyList<string>? participantNames = null;

            if (assignment.EnrollmentGroup is not null)
            {
                var leader = assignment.EnrollmentGroup.Members
                    .FirstOrDefault(m => m.Id == assignment.EnrollmentGroup.LeaderEnrollmentId)
                    ?? assignment.EnrollmentGroup.Members.FirstOrDefault();
                if (leader is null) return;
                toEmail = leader.ContactEmail;
                toName = assignment.EnrollmentGroup.Name;
                participantNames = assignment.EnrollmentGroup.Members.Select(m => m.StudentName).ToList();
            }
            else if (assignment.Enrollment is not null)
            {
                toEmail = assignment.Enrollment.ContactEmail;
                toName = assignment.Enrollment.StudentName;
            }
            else
            {
                return;
            }

            await emailService.SendAssignmentMovedAsync(
                toEmail, toName, series.Name,
                oldEntry.DayOfWeek, oldEntry.StartTime.ToString("HH:mm"), oldEntry.EndTime.ToString("HH:mm"),
                newEntry.DayOfWeek, newEntry.StartTime.ToString("HH:mm"), newEntry.EndTime.ToString("HH:mm"),
                newEntry.CourtName, participantNames, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Verplaatst-mail mislukt voor toewijzing {AssignmentId} (verplaatsing is wel gelukt).",
                assignment.Id);
        }
    }

    public async Task<Result<bool>> DeleteAssignmentAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default)
    {
        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status == ScheduleAssignmentStatus.Confirmed)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Bevestigde toewijzingen kunnen niet verwijderd worden."));

        scheduleAssignmentRepo.RemoveRange([assignment]);
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<Guid>> CreateGroupAsync(
        Guid seriesId, CreateGroupRequest request, Guid organizationId, CancellationToken ct = default)
    {
        var exists = await lessonSeriesRepo.ExistsAsync(seriesId, organizationId, ct);
        if (!exists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var selected = enrollments.Where(e => request.EnrollmentIds.Contains(e.Id)).ToList();

        if (selected.Count != request.EnrollmentIds.Count)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "Eén of meer inschrijvingen niet gevonden."));

        if (selected.Any(e => e.EnrollmentGroupId.HasValue))
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "Eén of meer inschrijvingen zitten al in een groep."));

        var existingGroupCount = await enrollmentGroupRepo.CountBySeriesAsync(seriesId, organizationId, ct);
        var groupLetter = (char)('A' + existingGroupCount);

        EnrollmentGroup group = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            Name = $"Groep {groupLetter}",
            LeaderEnrollmentId = selected[0].Id,
        };

        await enrollmentGroupRepo.AddAsync(group, ct);

        foreach (var enrollment in selected)
            enrollment.EnrollmentGroupId = group.Id;

        await enrollmentGroupRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(group.Id);
    }

    public async Task<Result<bool>> DissolveGroupAsync(
        Guid seriesId, Guid groupId, Guid organizationId, CancellationToken ct = default)
    {
        var group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        foreach (var member in group.Members)
            member.EnrollmentGroupId = null;

        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var groupAssignments = assignments.Where(a => a.EnrollmentGroupId == groupId).ToList();
        if (groupAssignments.Count > 0)
            scheduleAssignmentRepo.RemoveRange(groupAssignments);

        enrollmentGroupRepo.Delete(group);
        await enrollmentGroupRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RemoveMemberFromGroupAsync(
        Guid seriesId, Guid groupId, Guid enrollmentId, Guid organizationId,
        bool cancelEnrollment = false, CancellationToken ct = default)
    {
        EnrollmentGroup? group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        Enrollment? member = group.Members.FirstOrDefault(m => m.Id == enrollmentId);
        if (member is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Dit lid zit niet in deze groep."));

        // Gate: een betaalde/bevestigde groep niet meer herschikken (de groep deelt de status).
        // Geldt voor beide modes; annuleren van een betaalde inschrijving loopt via de aparte flow.
        if (member.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
            return Result<bool>.Fail(new Error(ErrorCodes.Conflict,
                "Dit lid kan niet uit de groep gehaald worden: de groep is al betaald of bevestigd."));

        // Detach het lid uit de groep. Bij cancelEnrollment wordt het lid bovendien geannuleerd
        // (soft-cancel: status Cancelled, formulierantwoorden blijven); anders blijft het een
        // actieve losse (solo) inschrijving.
        member.EnrollmentGroupId = null;
        if (cancelEnrollment)
            member.Status = EnrollmentStatus.Cancelled;

        List<Enrollment> remaining = group.Members.Where(m => m.Id != enrollmentId).ToList();

        if (remaining.Count <= 1)
        {
            // Ontbinden: laatste lid (indien er één is) wordt ook solo, en behoudt z'n planningsplek
            // doordat de groeps-toewijzing omgezet wordt naar een individuele toewijzing.
            Enrollment? last = remaining.FirstOrDefault();
            if (last is not null)
                last.EnrollmentGroupId = null;

            List<ScheduleAssignment> groupAssignments =
                (await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct))
                .Where(a => a.EnrollmentGroupId == groupId)
                .ToList();

            if (groupAssignments.Count > 0)
            {
                // Verwijder via key-only stubs (GetBySeriesAsync is AsNoTracking mét includes:
                // de include-dragende instances zouden botsen met de getrackte group/members).
                scheduleAssignmentRepo.RemoveRange(
                    groupAssignments.Select(a => new ScheduleAssignment { Id = a.Id }).ToList());

                if (last is not null)
                {
                    // Zet elke groeps-toewijzing om naar een individuele toewijzing voor het overblijvende lid.
                    List<ScheduleAssignment> individual = groupAssignments.Select(a => new ScheduleAssignment
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = a.OrganizationId,
                        LessonSerieId = a.LessonSerieId,
                        WeeklyTemplateEntryId = a.WeeklyTemplateEntryId,
                        EnrollmentGroupId = null,
                        EnrollmentId = last.Id,
                        Status = a.Status,
                        IsAutoMerged = false,
                        IsLocked = a.IsLocked,
                    }).ToList();
                    await scheduleAssignmentRepo.AddRangeAsync(individual, ct);
                }
            }

            enrollmentGroupRepo.Delete(group);
        }
        else
        {
            // ≥2 leden blijven: groeps-toewijzing ongemoeid (het lid valt er automatisch uit).
            // Was het verwijderde lid de leider, promoveer het vroegst-ingeschreven overblijvende lid.
            if (group.LeaderEnrollmentId == enrollmentId)
            {
                Enrollment newLeader = remaining
                    .OrderBy(m => m.EnrolledAt)
                    .ThenBy(m => m.StudentName)
                    .First();
                group.LeaderEnrollmentId = newLeader.Id;
            }
        }

        await enrollmentGroupRepo.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    private async Task<Error?> EnsureSlotCapacityAsync(
        Guid seriesId, Guid organizationId, Guid slotId, int addSize,
        Guid? excludeAssignmentId, CancellationToken ct)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        var slot = series?.WeeklyTemplate.FirstOrDefault(s => s.Id == slotId);
        if (slot is null) return null;

        var existing = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var currentCount = existing
            .Where(a => a.WeeklyTemplateEntryId == slotId && a.Id != excludeAssignmentId)
            .Sum(PlanningProposalBuilder.GetEffectiveAssignmentSize);

        if (currentCount + addSize > slot.MaxStudents)
            return new Error(ErrorCodes.Validation,
                $"Tijdslot heeft geen plaats meer ({currentCount}/{slot.MaxStudents}).");

        return null;
    }
}
