using Domain.Enums;

namespace Application.Organizations.Dtos;

public record MemberResponse(
    Guid Id,
    Guid UserId,
    string Email,
    MemberRole Role,
    DateTime JoinedAt
);
