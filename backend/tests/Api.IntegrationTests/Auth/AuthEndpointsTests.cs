using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Application.Auth.Dtos;

namespace Api.IntegrationTests.Auth;

public class AuthEndpointsTests : IntegrationTestBase
{
    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns200WithTokens()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "test@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var payload = new { email = "dupe@example.com", password = "Password123!" };
        await Client.PostAsJsonAsync("/auth/register", payload);

        var response = await Client.PostAsJsonAsync("/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "weak@example.com",
            password = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var email = "login@example.com";
        var password = "Password123!";
        await Client.PostAsJsonAsync("/auth/register", new { email, password });

        var response = await Client.PostAsJsonAsync("/auth/login", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "valid@example.com",
            password = "CorrectPassword123!"
        });

        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "valid@example.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "nobody@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var registerResponse = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "refresh@example.com",
            password = "Password123!"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = auth!.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth!.RefreshToken.Should().NotBe(auth.RefreshToken); // token rotated
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = "totally-invalid-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UsedToken_Returns401()
    {
        var registerResponse = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "rotate@example.com",
            password = "Password123!"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Use the refresh token once
        await Client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth!.RefreshToken });

        // Reuse the same token — should be revoked
        var response = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ForgotPassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_AnyEmail_Returns202()
    {
        // Even for unknown emails — prevent enumeration
        var response = await Client.PostAsJsonAsync("/auth/forgot-password", new
        {
            email = "doesnotexist@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
