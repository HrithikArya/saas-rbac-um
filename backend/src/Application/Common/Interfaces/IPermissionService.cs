using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid orgId, string permission, CancellationToken ct = default);
    Task<MemberRole?> GetRoleAsync(Guid userId, Guid orgId, CancellationToken ct = default);
    Task InvalidateCacheAsync(Guid userId, Guid orgId, CancellationToken ct = default);
}
