namespace Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid orgId, Guid actorUserId, string action, object? metadata = null, CancellationToken ct = default);
}
