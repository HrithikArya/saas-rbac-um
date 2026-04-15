using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Organizations;
using Application.Organizations.Dtos;
using Application.UnitTests.TestInfrastructure;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Organizations;

public class OrganizationServiceTests
{
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

    private static IAppDbContext CreateDb(string name) => TestDbContext.Create(name);

    private OrganizationService CreateSut(IAppDbContext db)
        => new OrganizationService(db, _tokenService, _emailService, _auditService, _permissionService);

    // ── Create org ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_NewOrg_ReturnsOrgAndCreatorIsOwner()
    {
        var db = CreateDb(nameof(Create_NewOrg_ReturnsOrgAndCreatorIsOwner));
        var userId = Guid.NewGuid();
        var sut = CreateSut(db);

        var result = await sut.CreateAsync(userId, new CreateOrgRequest("Acme Corp"));

        result.Name.Should().Be("Acme Corp");
        result.Slug.Should().Be("acme-corp");
        result.MemberCount.Should().Be(1);

        var member = await db.OrganizationMembers.SingleAsync();
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(MemberRole.Owner);
    }

    [Fact]
    public async Task Create_DuplicateName_GeneratesUniqueSlug()
    {
        var db = CreateDb(nameof(Create_DuplicateName_GeneratesUniqueSlug));
        var userId = Guid.NewGuid();
        var sut = CreateSut(db);

        await sut.CreateAsync(userId, new CreateOrgRequest("My Org"));
        var second = await sut.CreateAsync(userId, new CreateOrgRequest("My Org"));

        second.Slug.Should().Be("my-org-2");
    }

    // ── List orgs ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListForUser_ReturnsOnlyUsersOrgs()
    {
        var db = CreateDb(nameof(ListForUser_ReturnsOnlyUsersOrgs));
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var sut = CreateSut(db);

        await sut.CreateAsync(userA, new CreateOrgRequest("Org A"));
        await sut.CreateAsync(userB, new CreateOrgRequest("Org B"));

        var result = await sut.ListForUserAsync(userA);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Org A");
    }

    // ── Get org ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_NonMember_ThrowsNotFound()
    {
        var db = CreateDb(nameof(Get_NonMember_ThrowsNotFound));
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var sut = CreateSut(db);

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("Private Org"));

        var act = () => sut.GetAsync(org.Id, outsider);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404);
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvite_ValidRequest_SendsEmailAndReturnsToken()
    {
        var db = CreateDb(nameof(CreateInvite_ValidRequest_SendsEmailAndReturnsToken));
        var owner = Guid.NewGuid();
        var sut = CreateSut(db);
        _tokenService.GenerateRefreshToken().Returns("invite-token-123");

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("Test Org"));
        var token = await sut.CreateInviteAsync(org.Id, owner,
            new InviteMemberRequest("invite@example.com", MemberRole.Member));

        token.Should().Be("invite-token-123");
        await _emailService.Received(1).SendInviteAsync(
            "invite@example.com", "Test Org", "invite-token-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInvite_AsOwnerRole_ThrowsBadRequest()
    {
        var db = CreateDb(nameof(CreateInvite_AsOwnerRole_ThrowsBadRequest));
        var owner = Guid.NewGuid();
        var sut = CreateSut(db);

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("Test Org"));

        var act = () => sut.CreateInviteAsync(org.Id, owner,
            new InviteMemberRequest("invite@example.com", MemberRole.Owner));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── Accept invite ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptInvite_Valid_CreatesMembership()
    {
        var db = CreateDb(nameof(AcceptInvite_Valid_CreatesMembership));
        var owner = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var sut = CreateSut(db);
        _tokenService.GenerateRefreshToken().Returns("valid-token");

        // Create the invitee user
        db.Users.Add(new User
        {
            Id = inviteeId,
            Email = "invitee@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("My Org"));
        await sut.CreateInviteAsync(org.Id, owner,
            new InviteMemberRequest("invitee@example.com", MemberRole.Admin));

        await sut.AcceptInviteAsync(inviteeId, new AcceptInviteRequest("valid-token"));

        var membership = await db.OrganizationMembers
            .SingleAsync(m => m.UserId == inviteeId);
        membership.Role.Should().Be(MemberRole.Admin);
    }

    [Fact]
    public async Task AcceptInvite_WrongEmail_ThrowsForbidden()
    {
        var db = CreateDb(nameof(AcceptInvite_WrongEmail_ThrowsForbidden));
        var owner = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var sut = CreateSut(db);
        _tokenService.GenerateRefreshToken().Returns("token-abc");

        db.Users.Add(new User
        {
            Id = differentUserId,
            Email = "different@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("My Org"));
        await sut.CreateInviteAsync(org.Id, owner,
            new InviteMemberRequest("other@example.com", MemberRole.Member));

        var act = () => sut.AcceptInviteAsync(differentUserId, new AcceptInviteRequest("token-abc"));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 403);
    }

    // ── Change role ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeRole_OwnerMember_ThrowsBadRequest()
    {
        var db = CreateDb(nameof(ChangeRole_OwnerMember_ThrowsBadRequest));
        var owner = Guid.NewGuid();
        var sut = CreateSut(db);

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("My Org"));
        var ownerMembership = await db.OrganizationMembers.SingleAsync(m => m.UserId == owner);

        var act = () => sut.ChangeMemberRoleAsync(
            org.Id, ownerMembership.Id, owner, new ChangeRoleRequest(MemberRole.Admin));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── Remove member ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveMember_Owner_ThrowsBadRequest()
    {
        var db = CreateDb(nameof(RemoveMember_Owner_ThrowsBadRequest));
        var owner = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var sut = CreateSut(db);

        var org = await sut.CreateAsync(owner, new CreateOrgRequest("My Org"));
        var ownerMembership = await db.OrganizationMembers.SingleAsync(m => m.UserId == owner);

        var act = () => sut.RemoveMemberAsync(org.Id, ownerMembership.Id, actor);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400);
    }
}
