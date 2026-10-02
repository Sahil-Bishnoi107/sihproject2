using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Organizations.AddCredential;

/// <summary>Add a certificate or registration to the organization's credential wallet (tracked for expiry).</summary>
public sealed record AddCredentialCommand(CredentialType Type, string Number, DateOnly? IssuedOn, DateOnly? ExpiresOn) : ICommand<CredentialDto>;
