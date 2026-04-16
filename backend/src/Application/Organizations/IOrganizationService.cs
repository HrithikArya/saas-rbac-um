using Application.Organizations.Dtos;

namespace Application.Organizations;

public interface IOrganizationService
{
    Task<OrgResponse> CreateAsync(Guid userId, CreateOrgRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<OrgResponse>> ListForUserAsync(Guid userId, CancellationToken ct = default);
    Task<OrgResponse> GetAsync(Guid orgId, Guid requestingUserId, CancellationToken ct = default);
    Task<IReadOnlyList<MemberResponse>> ListMembersAsync(Guid orgId, CancellationToken ct = default);
    Task<string> CreateInviteAsync(Guid orgId, Guid actorUserId, InviteMemberRequest request, CancellationToken ct = default);
    Task AcceptInviteAsync(Guid userId, AcceptInviteRequest request, CancellationToken ct = default);
    Task ChangeMemberRoleAsync(Guid orgId, Guid targetMemberId, Guid actorUserId, ChangeRoleRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid orgId, Guid targetMemberId, Guid actorUserId, CancellationToken ct = default);
    Task<OrgResponse> UpdateAsync(Guid orgId, Guid requestingUserId, UpdateOrgRequest request, CancellationToken ct = default);
    Task<SubscriptionResponse?> GetSubscriptionAsync(Guid orgId, Guid requestingUserId, CancellationToken ct = default);
}
