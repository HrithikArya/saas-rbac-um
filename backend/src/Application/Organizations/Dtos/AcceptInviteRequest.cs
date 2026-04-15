using System.ComponentModel.DataAnnotations;

namespace Application.Organizations.Dtos;

public record AcceptInviteRequest(
    [Required] string Token
);
