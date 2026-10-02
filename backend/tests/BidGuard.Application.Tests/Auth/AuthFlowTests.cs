using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Features.Commands.Auth.Login;
using BidGuard.Application.Features.Commands.Auth.RefreshToken;
using BidGuard.Application.Features.Commands.Auth.RegisterBidder;
using BidGuard.Application.Services.Identity;
using BidGuard.Application.Tests.TestSupport;
using BidGuard.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Application.Tests.Auth;

public class AuthFlowTests : IDisposable
{
    private readonly TestHarness _h = new();

    private AuthTokenIssuer Issuer => new(_h.Jwt, _h.RefreshTokens, _h.Time, _h.CurrentUser);

    private async Task<AuthResult> RegisterAsync() =>
        await new RegisterBidderCommandHandler(_h.Users, _h.Organizations, _h.Hasher, Issuer, _h.Audit, _h.Db)
            .Handle(new RegisterBidderCommand("owner@acme.in", "Str0ng!Pass", "Asha Rao", "Acme Pvt Ltd", "AAACA1234F", OrganizationCategory.MSE),
                CancellationToken.None);

    private RefreshTokenCommandHandler RefreshHandler() =>
        new(_h.RefreshTokens, _h.Users, _h.Jwt, Issuer, _h.Audit, _h.Db, _h.Time);

    [Fact]
    public async Task Register_creates_bidder_with_organization_and_tokens()
    {
        var result = await RegisterAsync();

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Role.Should().Be(UserRole.Bidder);
        result.User.OrganizationId.Should().NotBeNull();
        (await _h.Db.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Register_with_duplicate_email_conflicts()
    {
        await RegisterAsync();

        var act = RegisterAsync;

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        await RegisterAsync();
        var handler = new LoginCommandHandler(_h.Users, _h.Hasher, Issuer, _h.Db);

        var act = async () => await handler.Handle(new LoginCommand("owner@acme.in", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_email_is_case_insensitive()
    {
        await RegisterAsync();
        var handler = new LoginCommandHandler(_h.Users, _h.Hasher, Issuer, _h.Db);

        var result = await handler.Handle(new LoginCommand("OWNER@acme.in", "Str0ng!Pass"), CancellationToken.None);

        result.User.Email.Should().Be("owner@acme.in");
    }

    [Fact]
    public async Task Refresh_rotates_token_and_revokes_the_old_one()
    {
        var registered = await RegisterAsync();

        var refreshed = await RefreshHandler().Handle(new RefreshTokenCommand(registered.RefreshToken), CancellationToken.None);

        refreshed.RefreshToken.Should().NotBe(registered.RefreshToken);
        var old = await _h.Db.RefreshTokens.SingleAsync(t => t.TokenHash == _h.Jwt.HashToken(registered.RefreshToken));
        old.RevokedAt.Should().NotBeNull();
        old.ReplacedByTokenHash.Should().Be(_h.Jwt.HashToken(refreshed.RefreshToken));
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_the_whole_family()
    {
        var registered = await RegisterAsync();
        var refreshed = await RefreshHandler().Handle(new RefreshTokenCommand(registered.RefreshToken), CancellationToken.None);

        var reuse = async () => await RefreshHandler().Handle(new RefreshTokenCommand(registered.RefreshToken), CancellationToken.None);

        await reuse.Should().ThrowAsync<UnauthorizedException>();
        var newest = await _h.Db.RefreshTokens.SingleAsync(t => t.TokenHash == _h.Jwt.HashToken(refreshed.RefreshToken));
        newest.RevokedAt.Should().NotBeNull();
        (await _h.Db.AuditLogs.AnyAsync(a => a.Action == "RefreshTokenReuseDetected")).Should().BeTrue();
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var registered = await RegisterAsync();
        _h.Time.Advance(TimeSpan.FromDays(8));

        var act = async () => await RefreshHandler().Handle(new RefreshTokenCommand(registered.RefreshToken), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public void Access_token_carries_user_id_email_and_role_claims()
    {
        var user = new BidGuard.Domain.Entities.User("o@x.in", "h", "O", UserRole.Officer);

        var token = _h.Jwt.CreateAccessToken(user);
        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token.Token);

        jwt.GetClaim("user_id").Value.Should().Be(user.Id.ToString());
        jwt.GetClaim("email").Value.Should().Be("o@x.in");
        jwt.GetClaim("role").Value.Should().Be("Officer");
    }

    public void Dispose() => _h.Dispose();
}
