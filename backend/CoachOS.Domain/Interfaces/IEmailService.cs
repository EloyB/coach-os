namespace CoachOS.Domain.Interfaces;

public interface IEmailService
{
    Task SendTrainerInviteAsync(string toEmail, string firstName, string inviteUrl, CancellationToken ct = default);
}
