using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Interfaces.Verification;

public sealed record RegistryLookupResult(VerificationStatus Status, string? RegisteredName, string? DetailsJson);

/// <summary>Adapter to one government registry (GSTN, MCA21, Udyam, EPFO).</summary>
public interface IRegistryVerificationService
{
    VerificationSource Source { get; }
    Task<RegistryLookupResult> VerifyAsync(string identifier, CancellationToken ct = default);
}

public interface IGstVerificationService : IRegistryVerificationService;

public interface IMcaVerificationService : IRegistryVerificationService;

public interface IUdyamVerificationService : IRegistryVerificationService;

public interface IEpfoVerificationService : IRegistryVerificationService;
