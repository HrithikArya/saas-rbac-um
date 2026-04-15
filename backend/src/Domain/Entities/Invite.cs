using Domain.Enums;

namespace Domain.Entities;

public class Invite
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Email { get; set; }
    public MemberRole Role { get; set; }
    public required string Token { get; set; }
    public InviteStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
