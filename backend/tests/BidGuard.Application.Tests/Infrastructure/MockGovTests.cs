using BidGuard.Domain.Enums;
using BidGuard.Infrastructure.ExternalServices.MockGov;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace BidGuard.Application.Tests.Infrastructure;

public class MockGovTests
{
    private static readonly IOptions<MockGovSettings> NoLatency = Options.Create(new MockGovSettings { LatencyMs = 0 });

    [Fact]
    public async Task Valid_GSTIN_is_verified_with_registered_name()
    {
        var result = await new MockGstVerificationService(NoLatency).VerifyAsync("27AAECS1234K1Z5");

        result.Status.Should().Be(VerificationStatus.Verified);
        result.RegisteredName.Should().Be("Shakti Enterprises Pvt Ltd");
    }

    [Fact]
    public async Task Lowercase_input_is_normalised()
    {
        var result = await new MockGstVerificationService(NoLatency).VerifyAsync(" 27aaecs1234k1z5 ");

        result.Status.Should().Be(VerificationStatus.Verified);
    }

    [Fact]
    public async Task Malformed_GSTIN_is_not_found()
    {
        var result = await new MockGstVerificationService(NoLatency).VerifyAsync("27-NOT-A-GSTIN");

        result.Status.Should().Be(VerificationStatus.NotFound);
    }

    [Fact]
    public async Task Cancelled_registration_is_a_mismatch()
    {
        var result = await new MockGstVerificationService(NoLatency).VerifyAsync(MockRegistry.CancelledGstin);

        result.Status.Should().Be(VerificationStatus.Mismatch);
        result.DetailsJson.Should().Contain("Cancelled");
    }

    [Theory]
    [InlineData("UDYAM-MH-26-0012345", VerificationStatus.Verified)]
    [InlineData("UDYAM-MH-2-0012345", VerificationStatus.NotFound)]
    public async Task Udyam_format_is_checked(string number, VerificationStatus expected)
    {
        (await new MockUdyamVerificationService(NoLatency).VerifyAsync(number)).Status.Should().Be(expected);
    }

    [Theory]
    [InlineData("U72900MH2019PTC123456", VerificationStatus.Verified)]
    [InlineData("X72900MH2019PTC123456", VerificationStatus.NotFound)]
    public async Task Cin_format_is_checked(string cin, VerificationStatus expected)
    {
        (await new MockMcaVerificationService(NoLatency).VerifyAsync(cin)).Status.Should().Be(expected);
    }

    [Theory]
    [InlineData("MHBAN0012345000", VerificationStatus.Verified)]
    [InlineData("12345", VerificationStatus.NotFound)]
    public async Task Epfo_format_is_checked(string code, VerificationStatus expected)
    {
        (await new MockEpfoVerificationService(NoLatency).VerifyAsync(code)).Status.Should().Be(expected);
    }

    [Fact]
    public void Each_adapter_reports_its_source()
    {
        new MockGstVerificationService(NoLatency).Source.Should().Be(VerificationSource.GST);
        new MockMcaVerificationService(NoLatency).Source.Should().Be(VerificationSource.MCA);
        new MockUdyamVerificationService(NoLatency).Source.Should().Be(VerificationSource.Udyam);
        new MockEpfoVerificationService(NoLatency).Source.Should().Be(VerificationSource.EPFO);
    }
}
