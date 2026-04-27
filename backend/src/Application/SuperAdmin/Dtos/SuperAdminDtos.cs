namespace Application.SuperAdmin.Dtos;

public record SuperAdminStatsDto(
    int TotalOrgs,
    int TotalUsers,
    int TotalRevenueCents,
    int ActiveSubscriptions
);

public record PlanDto(Guid Id, string Name, int PriceInCents);

public record OrgSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string OwnerEmail,
    int MemberCount,
    string PlanName,
    string? SubscriptionStatus,
    int PlanPriceInCents,
    Guid? PlanId,
    DateTime CreatedAt
);

public record MemberSummaryDto(Guid UserId, string Email, string Role);

public record PaymentSummaryDto(Guid Id, string PlanName, int AmountCents, string Status, DateTime CreatedAt);

public record OrgDetailDto(
    OrgSummaryDto Summary,
    IEnumerable<MemberSummaryDto> Members,
    IEnumerable<PaymentSummaryDto> Payments,
    IEnumerable<PlanDto> AvailablePlans
);

public record MonthlyEarningDto(int Year, int Month, string MonthLabel, int AmountCents, int PaymentCount);

public record OrgEarningDto(Guid OrgId, string OrgName, int TotalAmountCents, int PaymentCount);

public record EarningsDto(
    int TotalRevenueCents,
    int TotalPayments,
    IEnumerable<MonthlyEarningDto> Monthly,
    IEnumerable<OrgEarningDto> ByOrg,
    IEnumerable<PaymentSummaryDto> RecentPayments
);

public record SuperAdminCreateOrgRequest(string OrgName, string OwnerEmail);

public record MockConfirmRequest(Guid OrgId, string PriceId);
