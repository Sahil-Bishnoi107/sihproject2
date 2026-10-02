using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Dtos;

public sealed record CredentialDto(Guid Id, CredentialType Type, string Number, DateOnly? IssuedOn, DateOnly? ExpiresOn)
{
    public static CredentialDto From(Credential c) => new(c.Id, c.Type, c.Number, c.IssuedOn, c.ExpiresOn);
}

public sealed record OrganizationDto(
    Guid Id,
    string LegalName,
    string Pan,
    string? Gstin,
    string? UdyamNumber,
    string? Cin,
    string? EpfoCode,
    OrganizationCategory Category,
    bool IsStartup,
    string? DpiitNumber,
    IReadOnlyList<CredentialDto> Credentials)
{
    public static OrganizationDto From(Organization o) => new(o.Id, o.LegalName, o.Pan, o.Gstin, o.UdyamNumber, o.Cin, o.EpfoCode,
        o.Category, o.IsStartup, o.DpiitNumber, o.Credentials.OrderBy(c => c.ExpiresOn).Select(CredentialDto.From).ToList());
}

public sealed record NotificationDto(Guid Id, NotificationType Type, string Message, string? ReferenceId, bool IsRead, DateTime CreatedAt)
{
    public static NotificationDto From(Notification n) => new(n.Id, n.Type, n.Message, n.ReferenceId, n.IsRead, n.CreatedAt);
}

public sealed record DocumentContent(Stream Content, string FileName, string ContentType);
