using Application.SuperAdmin.Dtos;

namespace Application.SuperAdmin;

public interface ISuperAdminService
{
    Task<SuperAdminStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<IEnumerable<OrgSummaryDto>> GetOrgsAsync(CancellationToken ct = default);
    Task<OrgDetailDto> GetOrgDetailAsync(Guid orgId, CancellationToken ct = default);
    Task ChangePlanAsync(Guid orgId, Guid planId, CancellationToken ct = default);
    Task<EarningsDto> GetEarningsAsync(CancellationToken ct = default);
    Task<OrgSummaryDto> CreateOrgAsync(SuperAdminCreateOrgRequest request, CancellationToken ct = default);
    Task<PaymentSummaryDto> ConfirmMockPaymentAsync(MockConfirmRequest request, CancellationToken ct = default);
}
