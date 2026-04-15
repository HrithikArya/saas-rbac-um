namespace Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<OrganizationMember> Members { get; set; } = [];
    public ICollection<Invite> Invites { get; set; } = [];
    public Subscription? Subscription { get; set; }
    public ICollection<AuditEvent> AuditEvents { get; set; } = [];
}
