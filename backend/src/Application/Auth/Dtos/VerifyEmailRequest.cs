using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Dtos;

public record VerifyEmailRequest(
    [Required] string Token
);
