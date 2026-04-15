using Application.Common.Constants;
using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IAppDbContext _db;
    private readonly ITokenStore _tokenStore;

    public PermissionService(IAppDbContext db, ITokenStore tokenStore)
    {
        _db = db;
        _tokenStore = tokenStore;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid orgId, string permission, CancellationToken ct = default)
    {
        var role = await GetRoleAsync(userId, orgId, ct);
        return role is not null && Permissions.ForRole(role.Value).Contains(permission);
    }

    public async Task<MemberRole?> GetRoleAsync(Guid userId, Guid orgId, CancellationToken ct = default)
    {
        // Try Redis cache (5-minute TTL)
        var cacheKey = CacheKey(userId, orgId);
        var cached = await _tokenStore.GetAsync(cacheKey, ct);
        if (cached is not null && Enum.TryParse<MemberRole>(cached, out var cachedRole))
            return cachedRole;

        // Miss — hit database
        var member = await _db.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId, ct);

        if (member is null) return null;

        await _tokenStore.SetAsync(cacheKey, member.Role.ToString(), TimeSpan.FromMinutes(5), ct);
        return member.Role;
    }

    public async Task InvalidateCacheAsync(Guid userId, Guid orgId, CancellationToken ct = default)
    {
        await _tokenStore.DeleteAsync(CacheKey(userId, orgId), ct);
    }

    private static string CacheKey(Guid userId, Guid orgId) => $"perm:{userId}:{orgId}";
}
