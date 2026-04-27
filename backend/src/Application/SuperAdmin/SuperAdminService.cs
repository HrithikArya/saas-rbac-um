using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.SuperAdmin.Dtos;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.SuperAdmin;

public class SuperAdminService : ISuperAdminService
{
    private readonly IAppDbContext _db;

    public SuperAdminService(IAppDbContext db) => _db = db;

    public async Task<SuperAdminStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var totalOrgs = await _db.Organizations.CountAsync(ct);
        var totalUsers = await _db.Users.CountAsync(ct);
        var totalRevenue = await _db.Payments.SumAsync(p => (int?)p.AmountInCents, ct) ?? 0;
        var activeSubs = await _db.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing, ct);

        return new SuperAdminStatsDto(totalOrgs, totalUsers, totalRevenue, activeSubs);
    }

    public async Task<IEnumerable<OrgSummaryDto>> GetOrgsAsync(CancellationToken ct = default)
    {
        var orgs = await _db.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
            .Include(o => o.Subscription)
                .ThenInclude(s => s!.Plan)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orgs.Select(ToSummary);
    }

    public async Task<OrgDetailDto> GetOrgDetailAsync(Guid orgId, CancellationToken ct = default)
    {
        var org = await _db.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members).ThenInclude(m => m.User)
            .Include(o => o.Subscription).ThenInclude(s => s!.Plan)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new AppException("Organization not found", 404);

        var payments = await _db.Payments
            .Include(p => p.Plan)
            .Where(p => p.OrganizationId == orgId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var plans = await _db.Plans.OrderBy(p => p.PriceInCents).ToListAsync(ct);

        var members = org.Members.Select(m => new MemberSummaryDto(
            m.UserId, m.User.Email, m.Role.ToString()
        ));

        var paymentDtos = payments.Select(p => new PaymentSummaryDto(
            p.Id, p.Plan.Name, p.AmountInCents, p.Status.ToString(), p.CreatedAt
        ));

        var planDtos = plans.Select(p => new PlanDto(p.Id, p.Name, p.PriceInCents));

        return new OrgDetailDto(ToSummary(org), members, paymentDtos, planDtos);
    }

    public async Task ChangePlanAsync(Guid orgId, Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FindAsync([planId], ct)
            ?? throw new AppException("Plan not found", 404);

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub is null)
        {
            sub = new Subscription
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                PlanId = planId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Subscriptions.Add(sub);
        }
        else
        {
            sub.PlanId = planId;
            sub.Status = SubscriptionStatus.Active;
            sub.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<EarningsDto> GetEarningsAsync(CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .Include(p => p.Plan)
            .Include(p => p.Organization)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var total = payments.Sum(p => p.AmountInCents);

        var monthly = payments
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Select(g => new MonthlyEarningDto(
                g.Key.Year,
                g.Key.Month,
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                g.Sum(p => p.AmountInCents),
                g.Count()
            ));

        var byOrg = payments
            .GroupBy(p => new { p.OrganizationId, p.Organization.Name })
            .OrderByDescending(g => g.Sum(p => p.AmountInCents))
            .Select(g => new OrgEarningDto(
                g.Key.OrganizationId,
                g.Key.Name,
                g.Sum(p => p.AmountInCents),
                g.Count()
            ));

        var recent = payments.Take(20).Select(p => new PaymentSummaryDto(
            p.Id, p.Plan.Name, p.AmountInCents, p.Status.ToString(), p.CreatedAt
        ));

        return new EarningsDto(total, payments.Count, monthly, byOrg, recent);
    }

    public async Task<OrgSummaryDto> CreateOrgAsync(SuperAdminCreateOrgRequest request, CancellationToken ct = default)
    {
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.OwnerEmail.ToLowerInvariant(), ct)
            ?? throw new AppException($"User '{request.OwnerEmail}' not found. They must register first.", 404);

        var slug = request.OrgName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        if (await _db.Organizations.AnyAsync(o => o.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.OrgName,
            Slug = slug,
            OwnerId = owner.Id,
            CreatedAt = DateTime.UtcNow
        };

        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = owner.Id,
            Role = MemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        _db.Organizations.Add(org);
        _db.OrganizationMembers.Add(member);
        await _db.SaveChangesAsync(ct);

        org.Owner = owner;
        org.Members = [member];
        member.User = owner;
        return ToSummary(org);
    }

    public async Task<PaymentSummaryDto> ConfirmMockPaymentAsync(MockConfirmRequest request, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.StripePriceId == request.PriceId, ct)
            ?? throw new AppException($"Plan '{request.PriceId}' not found", 404);

        var org = await _db.Organizations.FindAsync([request.OrgId], ct)
            ?? throw new AppException("Organization not found", 404);

        // Upsert subscription
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == request.OrgId, ct);
        if (sub is null)
        {
            sub = new Subscription
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrgId,
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Subscriptions.Add(sub);
        }
        else
        {
            sub.PlanId = plan.Id;
            sub.Status = SubscriptionStatus.Active;
            sub.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
            sub.UpdatedAt = DateTime.UtcNow;
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrgId,
            PlanId = plan.Id,
            AmountInCents = plan.PriceInCents,
            Status = Domain.Entities.PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        return new PaymentSummaryDto(payment.Id, plan.Name, payment.AmountInCents, payment.Status.ToString(), payment.CreatedAt);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static OrgSummaryDto ToSummary(Organization org) => new(
        org.Id,
        org.Name,
        org.Slug,
        org.Owner?.Email ?? "",
        org.Members?.Count ?? 0,
        org.Subscription?.Plan?.Name ?? "Free",
        org.Subscription?.Status.ToString(),
        org.Subscription?.Plan?.PriceInCents ?? 0,
        org.Subscription?.PlanId,
        org.CreatedAt
    );
}
