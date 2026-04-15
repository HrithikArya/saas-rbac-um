using Application.Auth;
using Application.Auth.Dtos;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.UnitTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IPasswordHasher _passwordHasher;

    public AuthServiceTests()
    {
        // Use real BCrypt for password tests to verify hashing actually works
        _passwordHasher = new RealPasswordHasher();

        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("fake-access-token");
        _tokenService.GenerateRefreshToken()
            .Returns("fake-refresh-token");
    }

    private AuthService CreateSut(string dbName)
    {
        var db = TestDbContext.Create(dbName);
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-must-be-32-chars!!",
            ExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        });
        return new AuthService(db, _tokenService, _tokenStore, _emailService, _passwordHasher, jwtSettings);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewEmail_CreatesUserAndReturnsTokens()
    {
        var db = TestDbContext.Create(nameof(Register_NewEmail_CreatesUserAndReturnsTokens));
        var sut = new AuthService(db, _tokenService, _tokenStore, _emailService, _passwordHasher,
            Options.Create(new JwtSettings { Secret = "test-secret-key-32chars!!", ExpiryMinutes = 15, RefreshTokenExpiryDays = 7 }));

        var result = await sut.RegisterAsync(new RegisterRequest("new@example.com", "Password123!"));

        result.AccessToken.Should().Be("fake-access-token");
        result.RefreshToken.Should().Be("fake-refresh-token");
        result.User.Email.Should().Be("new@example.com");

        var savedUser = await db.Users.SingleAsync();
        savedUser.Email.Should().Be("new@example.com");
        savedUser.EmailVerified.Should().BeFalse();
        savedUser.PasswordHash.Should().NotBe("Password123!"); // must be hashed
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        var sut = CreateSut(nameof(Register_DuplicateEmail_ThrowsConflict));
        await sut.RegisterAsync(new RegisterRequest("dupe@example.com", "Password123!"));

        var act = () => sut.RegisterAsync(new RegisterRequest("dupe@example.com", "Password123!"));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLower()
    {
        var sut = CreateSut(nameof(Register_NormalizesEmailToLower));
        var result = await sut.RegisterAsync(new RegisterRequest("UPPER@EXAMPLE.COM", "Password123!"));

        result.User.Email.Should().Be("upper@example.com");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var sut = CreateSut(nameof(Login_ValidCredentials_ReturnsTokens));
        await sut.RegisterAsync(new RegisterRequest("login@example.com", "Password123!"));

        var result = await sut.LoginAsync(new LoginRequest("login@example.com", "Password123!"));

        result.AccessToken.Should().Be("fake-access-token");
        result.User.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var sut = CreateSut(nameof(Login_WrongPassword_ThrowsUnauthorized));
        await sut.RegisterAsync(new RegisterRequest("user@example.com", "CorrectPass123!"));

        var act = () => sut.LoginAsync(new LoginRequest("user@example.com", "WrongPass!"));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 401);
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized()
    {
        var sut = CreateSut(nameof(Login_UnknownEmail_ThrowsUnauthorized));

        var act = () => sut.LoginAsync(new LoginRequest("ghost@example.com", "Password123!"));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 401);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_DeletesRefreshToken()
    {
        var sut = CreateSut(nameof(Logout_DeletesRefreshToken));
        await sut.LogoutAsync("some-refresh-token");

        await _tokenStore.Received(1).DeleteAsync(
            "refresh:some-refresh-token", Arg.Any<CancellationToken>());
    }

    // ── ForgotPassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNotThrow()
    {
        var sut = CreateSut(nameof(ForgotPassword_UnknownEmail_DoesNotThrow));

        var act = () => sut.ForgotPasswordAsync(new ForgotPasswordRequest("nobody@example.com"));

        await act.Should().NotThrowAsync();
        await _emailService.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

// Simple real BCrypt wrapper for tests — avoids mocking crypto
file sealed class RealPasswordHasher : Application.Common.Interfaces.IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
