using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAppDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(Guid orgId, Guid actorUserId, string action, object? metadata = null, CancellationToken ct = default)
    {
        var metaJson = metadata is null ? "{}" : JsonSerializer.Serialize(metadata);

        _db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ActorUserId = actorUserId,
            Action = action,
            MetadataJson = metaJson,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit failures must never break the main operation
            _logger.LogError(ex, "Failed to write audit event {Action} for org {OrgId}", action, orgId);
        }
    }
}
