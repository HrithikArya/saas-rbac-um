using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Dtos;

public record LogoutRequest(
    [Required] string RefreshToken
);
