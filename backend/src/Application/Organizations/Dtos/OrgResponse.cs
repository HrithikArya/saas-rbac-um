namespace Application.Organizations.Dtos;

public record OrgResponse(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    int MemberCount
);
