namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool EmailVerified { get; set; }
    public bool IsSuperAdmin { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<OrganizationMember> Memberships { get; set; } = [];
    public ICollection<Organization> OwnedOrganizations { get; set; } = [];
}
