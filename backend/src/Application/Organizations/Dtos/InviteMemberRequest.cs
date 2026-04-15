using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.Organizations.Dtos;

public record InviteMemberRequest(
    [Required, EmailAddress] string Email,
    MemberRole Role = MemberRole.Member
);
