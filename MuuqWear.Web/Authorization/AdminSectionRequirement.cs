using Microsoft.AspNetCore.Authorization;

namespace MuuqWear.Web.Authorization;

public sealed class AdminSectionRequirement(string section) : IAuthorizationRequirement
{
    public string Section { get; } = section;
}
