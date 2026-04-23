using Application.Auth.Dtos;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Auth;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITokenStore _tokenStore;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IAppDbContext db,
        ITokenService tokenService,
        ITokenStore tokenStore,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _tokenService = tokenService;
        _tokenStore = tokenStore;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new AppException("Email already registered", 409);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var verifyToken = _tokenService.GenerateRefreshToken();
        await _tokenStore.SetAsync(
            $"email_verify:{verifyToken}",
            user.Id.ToString(),
            TimeSpan.FromHours(24),
            ct);

        await _emailService.SendEmailVerificationAsync(user.Email, verifyToken, ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new AppException("Invalid credentials", 401);

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AppException("Invalid credentials", 401);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var key = $"refresh:{request.RefreshToken}";
        var userIdStr = await _tokenStore.GetAsync(key, ct)
            ?? throw new AppException("Invalid or expired refresh token", 401);

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new AppException("User not found", 401);

        // Rotate — delete old token before issuing new one
        await _tokenStore.DeleteAsync(key, ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        await _tokenStore.DeleteAsync($"refresh:{refreshToken}", ct);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var key = $"email_verify:{request.Token}";
        var userIdStr = await _tokenStore.GetAsync(key, ct)
            ?? throw new AppException("Invalid or expired verification token", 400);

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new AppException("User not found", 400);

        user.EmailVerified = true;
        await _db.SaveChangesAsync(ct);
        await _tokenStore.DeleteAsync(key, ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Always succeed — prevents email enumeration
        if (user is null) return;

        var resetToken = _tokenService.GenerateRefreshToken();
        await _tokenStore.SetAsync(
            $"pwd_reset:{resetToken}",
            user.Id.ToString(),
            TimeSpan.FromHours(1),
            ct);

        await _emailService.SendPasswordResetAsync(user.Email, resetToken, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var key = $"pwd_reset:{request.Token}";
        var userIdStr = await _tokenStore.GetAsync(key, ct)
            ?? throw new AppException("Invalid or expired reset token", 400);

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new AppException("User not found", 400);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);
        await _tokenStore.DeleteAsync(key, ct);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.IsSuperAdmin);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _tokenStore.SetAsync(
            $"refresh:{refreshToken}",
            user.Id.ToString(),
            TimeSpan.FromDays(_jwtSettings.RefreshTokenExpiryDays),
            ct);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            User: new UserDto(user.Id, user.Email, user.EmailVerified, user.IsSuperAdmin)
        );
    }
}
