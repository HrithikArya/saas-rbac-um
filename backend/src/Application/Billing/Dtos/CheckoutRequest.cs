using System.ComponentModel.DataAnnotations;

namespace Application.Billing.Dtos;

public record CheckoutRequest(
    [Required] string PriceId
);
