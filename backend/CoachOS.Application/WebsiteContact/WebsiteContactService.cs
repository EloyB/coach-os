using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using FluentValidation;

namespace CoachOS.Application.WebsiteContact;

public sealed record SubmitWebsiteContactRequest(string Name, string Email, string Message, string? Organization = null);

public sealed class SubmitWebsiteContactValidator : AbstractValidator<SubmitWebsiteContactRequest>
{
    public SubmitWebsiteContactValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(2000);
        RuleFor(x => x.Organization).MaximumLength(100);
    }
}

public interface IWebsiteContactService
{
    Task SubmitAsync(SubmitWebsiteContactRequest request, CancellationToken ct);
}

public sealed class WebsiteContactService(IWebsiteContactRepository repository, IUnitOfWork unitOfWork, TimeProvider clock) : IWebsiteContactService
{
    public async Task SubmitAsync(SubmitWebsiteContactRequest request, CancellationToken ct)
    {
        await repository.AddAsync(new WebsiteContactEntry
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Email = request.Email.Trim().ToLowerInvariant(),
            Message = request.Message, Organization = request.Organization?.Trim(), CreatedAt = clock.GetUtcNow().UtcDateTime,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
