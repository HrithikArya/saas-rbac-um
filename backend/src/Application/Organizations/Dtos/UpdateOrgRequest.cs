using System.ComponentModel.DataAnnotations;

namespace Application.Organizations.Dtos;

public record UpdateOrgRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name
);
