using Domain.Enums;

namespace Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
