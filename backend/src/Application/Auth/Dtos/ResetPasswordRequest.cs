using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Dtos;

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(8)] string NewPassword
);
