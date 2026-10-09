using System.Security.Claims;
using ITAssetManagement.Services;
using Microsoft.AspNetCore.Authorization;

namespace ITAssetManagement.Authorization;

public sealed class PermissionAuthorizationHandler(PermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!int.TryParse(context.User.FindFirstValue("userId"), out var userId))
            return;

        var allowed = requirement.Permission switch
        {
            AppPermission.View => await permissionService.CanViewAsync(userId),
            AppPermission.Request => await permissionService.CanRequestAsync(userId),
            AppPermission.Approve => await permissionService.CanApproveAsync(userId),
            AppPermission.Admin => await permissionService.CanAdminAsync(userId),
            _ => false
        };

        if (allowed)
            context.Succeed(requirement);
    }
}
