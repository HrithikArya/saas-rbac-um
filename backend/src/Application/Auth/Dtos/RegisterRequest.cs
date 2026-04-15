using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);
