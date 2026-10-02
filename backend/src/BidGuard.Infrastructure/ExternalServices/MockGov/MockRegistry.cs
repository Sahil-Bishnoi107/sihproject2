using System.Text.Json;
using System.Text.RegularExpressions;
using BidGuard.Application.Common.Interfaces.Verification;
using BidGuard.Application.Common.Validation;
using BidGuard.Domain.Enums;
using Microsoft.Extensions.Options;

namespace BidGuard.Infrastructure.ExternalServices.MockGov;

public sealed class MockGovSettings
{
    public const string SectionName = "MockGov";

    /// <summary>Simulated registry round-trip time.</summary>
    public int LatencyMs { get; init; } = 200;
}

/// <summary>
/// Deterministic stand-in for GSTN / MCA21 / Udyam / EPFO. Format-valid identifiers verify, a known set
/// resolves to demo entity names, and <see cref="CancelledGstin"/> simulates a cancelled registration.
/// Swap for real API Setu / GSP adapters without touching the Application layer.
/// </summary>
public static partial class MockRegistry
{
    public const string CancelledGstin = "29AABCZ9999Z1Z9";

    [GeneratedRegex("^[A-Z]{5}[0-9]{7,10}$")]
    public static partial Regex EpfoRegex();

    public static readonly IReadOnlyDictionary<string, string> KnownEntities = new Dictionary<string, string>
    {
        ["27AAECS1234K1Z5"] = "Shakti Enterprises Pvt Ltd",
        ["UDYAM-MH-26-0012345"] = "Shakti Enterprises Pvt Ltd",
        ["U72900MH2019PTC123456"] = "Shakti Enterprises Pvt Ltd",
        ["MHBAN0012345000"] = "Shakti Enterprises Pvt Ltd",
        ["29AABCN5678P1Z2"] = "Navachar Technologies Pvt Ltd",
        ["UDYAM-KA-03-0098765"] = "Navachar Technologies Pvt Ltd",
    };

    public static readonly IReadOnlySet<string> Cancelled = new HashSet<string> { CancelledGstin };

    public static RegistryLookupResult Lookup(VerificationSource source, Regex format, string identifier)
    {
        var id = identifier.Trim().ToUpperInvariant();
        if (!format.IsMatch(id))
            return new RegistryLookupResult(VerificationStatus.NotFound, null,
                JsonSerializer.Serialize(new { source = source.ToString(), identifier = id, reason = "No record matches this identifier format" }));

        if (Cancelled.Contains(id))
            return new RegistryLookupResult(VerificationStatus.Mismatch, null,
                JsonSerializer.Serialize(new { source = source.ToString(), identifier = id, status = "Cancelled" }));

        var name = KnownEntities.GetValueOrDefault(id, "Registered Entity (mock registry)");
        return new RegistryLookupResult(VerificationStatus.Verified, name,
            JsonSerializer.Serialize(new { source = source.ToString(), identifier = id, status = "Active", legalName = name }));
    }
}

public abstract class MockRegistryVerificationService(IOptions<MockGovSettings> settings) : IRegistryVerificationService
{
    public abstract VerificationSource Source { get; }
    protected abstract Regex Format { get; }

    public async Task<RegistryLookupResult> VerifyAsync(string identifier, CancellationToken ct = default)
    {
        if (settings.Value.LatencyMs > 0)
            await Task.Delay(settings.Value.LatencyMs, ct);
        return MockRegistry.Lookup(Source, Format, identifier);
    }
}

public sealed class MockGstVerificationService(IOptions<MockGovSettings> s) : MockRegistryVerificationService(s), IGstVerificationService
{
    public override VerificationSource Source => VerificationSource.GST;
    protected override Regex Format => ValidationRules.GstinRegex();
}

public sealed class MockMcaVerificationService(IOptions<MockGovSettings> s) : MockRegistryVerificationService(s), IMcaVerificationService
{
    public override VerificationSource Source => VerificationSource.MCA;
    protected override Regex Format => ValidationRules.CinRegex();
}

public sealed class MockUdyamVerificationService(IOptions<MockGovSettings> s) : MockRegistryVerificationService(s), IUdyamVerificationService
{
    public override VerificationSource Source => VerificationSource.Udyam;
    protected override Regex Format => ValidationRules.UdyamRegex();
}

public sealed class MockEpfoVerificationService(IOptions<MockGovSettings> s) : MockRegistryVerificationService(s), IEpfoVerificationService
{
    public override VerificationSource Source => VerificationSource.EPFO;
    protected override Regex Format => MockRegistry.EpfoRegex();
}
