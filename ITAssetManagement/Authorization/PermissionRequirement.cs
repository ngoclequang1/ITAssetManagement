using Microsoft.AspNetCore.Authorization;

namespace ITAssetManagement.Authorization;

public enum AppPermission
{
    View,
    Request,
    Approve,
    Admin
}

public sealed record PermissionRequirement(AppPermission Permission) : IAuthorizationRequirement;

public static class PermissionPolicies
{
    public const string CanView = nameof(CanView);
    public const string CanRequest = nameof(CanRequest);
    public const string CanApprove = nameof(CanApprove);
    public const string CanAdmin = nameof(CanAdmin);
}
