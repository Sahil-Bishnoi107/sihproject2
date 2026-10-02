using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;

namespace BidGuard.Application.Jobs;

/// <summary>Daily: warn bidders about certificates/registrations expiring within 30 days, at most once a week each.</summary>
public sealed class CredentialExpiryAlertJob(
    ICredentialRepository credentials,
    IUserRepository users,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public const string RecurringId = "credential-expiry-alerts";
    public const string Cron = "0 6 * * *";

    private const int WindowDays = 30;
    private const int RepeatAfterDays = 7;

    public async Task RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var expiring = await credentials.GetExpiringAsync(today.AddDays(WindowDays), now.AddDays(-RepeatAfterDays), ct);
        foreach (var credential in expiring)
        {
            var days = credential.ExpiresOn!.Value.DayNumber - today.DayNumber;
            var when = days < 0 ? $"expired {-days} day(s) ago" : days == 0 ? "expires today" : $"expires in {days} day(s)";

            foreach (var user in await users.GetByOrganizationAsync(credential.OrganizationId, ct))
                await notifications.AddAsync(new Notification(user.Id, NotificationType.CredentialExpiring,
                    $"{credential.Type} {credential.Number} {when} (on {credential.ExpiresOn:yyyy-MM-dd}). Renew it before your next bid.",
                    credential.Id.ToString()), ct);

            credential.MarkExpiryAlerted(now);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
