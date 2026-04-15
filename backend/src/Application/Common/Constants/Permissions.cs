using Domain.Enums;

namespace Application.Common.Constants;

public static class Permissions
{
    public const string ProjectsRead  = "projects.read";
    public const string ProjectsWrite = "projects.write";
    public const string MembersManage = "members.manage";
    public const string BillingManage = "billing.manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ProjectsRead, ProjectsWrite, MembersManage, BillingManage
    };

    public static IReadOnlySet<string> ForRole(MemberRole role) => role switch
    {
        MemberRole.Owner  => All,
        MemberRole.Admin  => new HashSet<string> { ProjectsRead, ProjectsWrite, MembersManage },
        MemberRole.Member => new HashSet<string> { ProjectsRead, ProjectsWrite },
        MemberRole.Viewer => new HashSet<string> { ProjectsRead },
        _                 => new HashSet<string>()
    };
}
