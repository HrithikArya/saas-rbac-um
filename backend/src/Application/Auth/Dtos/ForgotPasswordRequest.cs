using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Dtos;

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);
