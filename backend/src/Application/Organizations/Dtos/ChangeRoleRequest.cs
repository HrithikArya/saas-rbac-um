using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.Organizations.Dtos;

public record ChangeRoleRequest(
    [Required] MemberRole Role
);
