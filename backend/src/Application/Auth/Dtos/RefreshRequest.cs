using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Dtos;

public record RefreshRequest(
    [Required] string RefreshToken
);
