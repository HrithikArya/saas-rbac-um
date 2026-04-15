namespace Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ActorUserId { get; set; }
    public required string Action { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
