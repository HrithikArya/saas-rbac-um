using System.Text.RegularExpressions;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Organizations.Dtos;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Organizations;

public class OrganizationService : IOrganizationService
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;

    public OrganizationService(
        IAppDbContext db,
        ITokenService tokenService,
        IEmailService emailService,
        IAuditService audit,
        IPermissionService permissions)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _audit = audit;
        _permissions = permissions;
    }

    // ── Create org ────────────────────────────────────────────────────────────

    public async Task<OrgResponse> CreateAsync(Guid userId, CreateOrgRequest request, CancellationToken ct = default)
    {
        var slug = await GenerateUniqueSlugAsync(request.Name, ct);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var membership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = org.Id,
            Role = MemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        _db.Organizations.Add(org);
        _db.OrganizationMembers.Add(membership);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(org.Id, userId, "org.created",
            new { orgId = org.Id, name = org.Name }, ct);

        return new OrgResponse(org.Id, org.Name, org.Slug, org.CreatedAt, 1);
    }

    // ── List orgs for user ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<OrgResponse>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Include(m => m.Organization)
            .Select(m => new OrgResponse(
                m.Organization.Id,
                m.Organization.Name,
                m.Organization.Slug,
                m.Organization.CreatedAt,
                m.Organization.Members.Count))
            .ToListAsync(ct);
    }

    // ── Get single org ────────────────────────────────────────────────────────

    public async Task<OrgResponse> GetAsync(Guid orgId, Guid requestingUserId, CancellationToken ct = default)
    {
        var isMember = await _db.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == orgId && m.UserId == requestingUserId, ct);

        if (!isMember)
            throw new AppException("Organization not found or access denied", 404);

        var org = await _db.Organizations
            .AsNoTracking()
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new AppException("Organization not found", 404);

        return new OrgResponse(org.Id, org.Name, org.Slug, org.CreatedAt, org.Members.Count);
    }

    // ── List members ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MemberResponse>> ListMembersAsync(Guid orgId, CancellationToken ct = default)
    {
        return await _db.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId)
            .Include(m => m.User)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new MemberResponse(m.Id, m.UserId, m.User.Email, m.Role, m.JoinedAt))
            .ToListAsync(ct);
    }

    // ── Create invite ─────────────────────────────────────────────────────────

    public async Task<string> CreateInviteAsync(Guid orgId, Guid actorUserId, InviteMemberRequest request, CancellationToken ct = default)
    {
        var org = await _db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new AppException("Organization not found", 404);

        // Cannot invite someone who is already a member
        var email = request.Email.ToLowerInvariant().Trim();
        var existingUser = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (existingUser is not null)
        {
            var alreadyMember = await _db.OrganizationMembers
                .AnyAsync(m => m.UserId == existingUser.Id && m.OrganizationId == orgId, ct);

            if (alreadyMember)
                throw new AppException("User is already a member of this organization", 409);
        }

        // Cannot invite as Owner
        if (request.Role == MemberRole.Owner)
            throw new AppException("Cannot invite a user as Owner", 400);

        // Expire any pending invites for the same email in this org
        var pending = await _db.Invites
            .Where(i => i.OrganizationId == orgId
                     && i.Email == email
                     && i.Status == InviteStatus.Pending)
            .ToListAsync(ct);

        foreach (var old in pending)
            old.Status = InviteStatus.Revoked;

        var token = _tokenService.GenerateRefreshToken(); // cryptographically random

        var invite = new Invite
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = email,
            Role = request.Role,
            Token = token,
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow
        };

        _db.Invites.Add(invite);
        await _db.SaveChangesAsync(ct);

        await _emailService.SendInviteAsync(email, org.Name, token, ct);

        await _audit.LogAsync(orgId, actorUserId, "invite.created",
            new { inviteId = invite.Id, email, role = request.Role.ToString() }, ct);

        return token;
    }

    // ── Accept invite ─────────────────────────────────────────────────────────

    public async Task AcceptInviteAsync(Guid userId, AcceptInviteRequest request, CancellationToken ct = default)
    {
        var invite = await _db.Invites
            .FirstOrDefaultAsync(i => i.Token == request.Token, ct)
            ?? throw new AppException("Invalid invite token", 400);

        if (invite.Status != InviteStatus.Pending)
            throw new AppException("Invite is no longer valid", 400);

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _db.SaveChangesAsync(ct);
            throw new AppException("Invite has expired", 400);
        }

        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new AppException("User not found", 400);

        // Verify the invite email matches
        if (!string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
            throw new AppException("Invite was sent to a different email address", 403);

        var alreadyMember = await _db.OrganizationMembers
            .AnyAsync(m => m.UserId == userId && m.OrganizationId == invite.OrganizationId, ct);

        if (alreadyMember)
            throw new AppException("Already a member of this organization", 409);

        var membership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = invite.OrganizationId,
            Role = invite.Role,
            JoinedAt = DateTime.UtcNow
        };

        invite.Status = InviteStatus.Accepted;
        _db.OrganizationMembers.Add(membership);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(invite.OrganizationId, userId, "invite.accepted",
            new { inviteId = invite.Id, role = invite.Role.ToString() }, ct);
    }

    // ── Change member role ────────────────────────────────────────────────────

    public async Task ChangeMemberRoleAsync(Guid orgId, Guid targetMemberId, Guid actorUserId, ChangeRoleRequest request, CancellationToken ct = default)
    {
        var target = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.Id == targetMemberId && m.OrganizationId == orgId, ct)
            ?? throw new AppException("Member not found", 404);

        if (target.Role == MemberRole.Owner)
            throw new AppException("Cannot change the role of the organization Owner", 400);

        if (request.Role == MemberRole.Owner)
            throw new AppException("Cannot promote a member to Owner via role change", 400);

        var previousRole = target.Role;
        target.Role = request.Role;
        await _db.SaveChangesAsync(ct);

        // Invalidate permission cache for this user
        await _permissions.InvalidateCacheAsync(target.UserId, orgId, ct);

        await _audit.LogAsync(orgId, actorUserId, "member.role_changed",
            new { memberId = targetMemberId, from = previousRole.ToString(), to = request.Role.ToString() }, ct);
    }

    // ── Remove member ─────────────────────────────────────────────────────────

    public async Task RemoveMemberAsync(Guid orgId, Guid targetMemberId, Guid actorUserId, CancellationToken ct = default)
    {
        var target = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.Id == targetMemberId && m.OrganizationId == orgId, ct)
            ?? throw new AppException("Member not found", 404);

        if (target.Role == MemberRole.Owner)
            throw new AppException("Cannot remove the organization Owner", 400);

        if (target.UserId == actorUserId)
            throw new AppException("Cannot remove yourself; transfer ownership first", 400);

        _db.OrganizationMembers.Remove(target);
        await _db.SaveChangesAsync(ct);

        await _permissions.InvalidateCacheAsync(target.UserId, orgId, ct);

        await _audit.LogAsync(orgId, actorUserId, "member.removed",
            new { memberId = targetMemberId, userId = target.UserId }, ct);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        var slug = baseSlug;
        var counter = 2;

        while (await _db.Organizations.AnyAsync(o => o.Slug == slug, ct))
            slug = $"{baseSlug}-{counter++}";

        return slug;
    }
}
