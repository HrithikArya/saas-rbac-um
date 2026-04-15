using System.ComponentModel.DataAnnotations;

namespace Application.Organizations.Dtos;

public record CreateOrgRequest(
    [Required, MinLength(2), MaxLength(100)] string Name
);
