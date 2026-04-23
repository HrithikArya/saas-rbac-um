namespace Domain.Entities;

public enum PaymentStatus { Paid, Refunded }

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public int AmountInCents { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Paid;
    public DateTime CreatedAt { get; set; }
}
