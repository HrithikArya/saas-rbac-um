using Application.Common.Constants;
using Application.Common.Interfaces;
using Application.UnitTests.TestInfrastructure;
using Domain.Enums;
using Infrastructure.Services;

namespace Application.UnitTests.Rbac;

/// <summary>
/// Tests the role → permission mapping defined in Permissions.ForRole.
/// These are pure logic tests — no I/O, no containers.
/// </summary>
public class PermissionsTests
{
    // ── Owner has every permission ─────────────────────────────────────────────

    [Theory]
    [InlineData(Permissions.ProjectsRead)]
    [InlineData(Permissions.ProjectsWrite)]
    [InlineData(Permissions.MembersManage)]
    [InlineData(Permissions.BillingManage)]
    public void Owner_HasAllPermissions(string permission)
    {
        Permissions.ForRole(MemberRole.Owner).Should().Contain(permission);
    }

    // ── Admin permissions ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(Permissions.ProjectsRead)]
    [InlineData(Permissions.ProjectsWrite)]
    [InlineData(Permissions.MembersManage)]
    public void Admin_HasProjectAndMemberPermissions(string permission)
    {
        Permissions.ForRole(MemberRole.Admin).Should().Contain(permission);
    }

    [Fact]
    public void Admin_DoesNotHaveBillingManage()
    {
        Permissions.ForRole(MemberRole.Admin).Should().NotContain(Permissions.BillingManage);
    }

    // ── Member permissions ────────────────────────────────────────────────────

    [Theory]
    [InlineData(Permissions.ProjectsRead)]
    [InlineData(Permissions.ProjectsWrite)]
    public void Member_HasProjectPermissions(string permission)
    {
        Permissions.ForRole(MemberRole.Member).Should().Contain(permission);
    }

    [Theory]
    [InlineData(Permissions.MembersManage)]
    [InlineData(Permissions.BillingManage)]
    public void Member_DoesNotHaveAdminPermissions(string permission)
    {
        Permissions.ForRole(MemberRole.Member).Should().NotContain(permission);
    }

    // ── Viewer permissions ────────────────────────────────────────────────────

    [Fact]
    public void Viewer_OnlyHasProjectsRead()
    {
        var perms = Permissions.ForRole(MemberRole.Viewer);
        perms.Should().ContainSingle().Which.Should().Be(Permissions.ProjectsRead);
    }

    [Theory]
    [InlineData(Permissions.ProjectsWrite)]
    [InlineData(Permissions.MembersManage)]
    [InlineData(Permissions.BillingManage)]
    public void Viewer_DoesNotHaveWriteOrManagePermissions(string permission)
    {
        Permissions.ForRole(MemberRole.Viewer).Should().NotContain(permission);
    }
}

/// <summary>
/// Tests PermissionService caching and DB fallback using in-memory EF + NSubstitute.
/// </summary>
public class PermissionServiceTests
{
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();

    private static IAppDbContext CreateDb(string name) => TestDbContext.Create(name);

    [Fact]
    public async Task GetRole_MemberExists_ReturnsMemberRole()
    {
        var db = CreateDb(nameof(GetRole_MemberExists_ReturnsMemberRole));
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        db.OrganizationMembers.Add(new Domain.Entities.OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            Role = MemberRole.Admin,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _tokenStore.GetAsync(Arg.Any<string>()).Returns((string?)null);

        var sut = new PermissionService(db, _tokenStore);
        var role = await sut.GetRoleAsync(userId, orgId);

        role.Should().Be(MemberRole.Admin);
    }

    [Fact]
    public async Task GetRole_NotAMember_ReturnsNull()
    {
        var db = CreateDb(nameof(GetRole_NotAMember_ReturnsNull));
        _tokenStore.GetAsync(Arg.Any<string>()).Returns((string?)null);

        var sut = new PermissionService(db, _tokenStore);
        var role = await sut.GetRoleAsync(Guid.NewGuid(), Guid.NewGuid());

        role.Should().BeNull();
    }

    [Fact]
    public async Task GetRole_CacheHit_DoesNotQueryDb()
    {
        var db = CreateDb(nameof(GetRole_CacheHit_DoesNotQueryDb));
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        // Cache already has a value
        _tokenStore.GetAsync($"perm:{userId}:{orgId}").Returns("Owner");

        var sut = new PermissionService(db, _tokenStore);
        var role = await sut.GetRoleAsync(userId, orgId);

        role.Should().Be(MemberRole.Owner);
        // DB was not queried (no member records exist in empty db, but we still got Owner from cache)
    }

    [Fact]
    public async Task HasPermission_AdminChecksProjectsWrite_ReturnsTrue()
    {
        var db = CreateDb(nameof(HasPermission_AdminChecksProjectsWrite_ReturnsTrue));
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        db.OrganizationMembers.Add(new Domain.Entities.OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            Role = MemberRole.Admin,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _tokenStore.GetAsync(Arg.Any<string>()).Returns((string?)null);

        var sut = new PermissionService(db, _tokenStore);
        var result = await sut.HasPermissionAsync(userId, orgId, Permissions.ProjectsWrite);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermission_ViewerChecksMembersManage_ReturnsFalse()
    {
        var db = CreateDb(nameof(HasPermission_ViewerChecksMembersManage_ReturnsFalse));
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        db.OrganizationMembers.Add(new Domain.Entities.OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            Role = MemberRole.Viewer,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _tokenStore.GetAsync(Arg.Any<string>()).Returns((string?)null);

        var sut = new PermissionService(db, _tokenStore);
        var result = await sut.HasPermissionAsync(userId, orgId, Permissions.MembersManage);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermission_NotAMember_ReturnsFalse()
    {
        var db = CreateDb(nameof(HasPermission_NotAMember_ReturnsFalse));
        _tokenStore.GetAsync(Arg.Any<string>()).Returns((string?)null);

        var sut = new PermissionService(db, _tokenStore);
        var result = await sut.HasPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), Permissions.ProjectsRead);

        result.Should().BeFalse();
    }
}
