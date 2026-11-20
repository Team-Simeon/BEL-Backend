using BEL.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace BEL.Api.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";

    public const string TeacherOrAdmin = "TeacherOrAdmin";

    public const string StudentOnly = "StudentOnly";

    // Creating, editing and publishing materials and tests: approved teachers and administrators.
    public const string CanPublishContent = "CanPublishContent";

    public static void AddBelPolicies(this AuthorizationOptions options)
    {
        // Endpoints without an explicit [Authorize]/[AllowAnonymous] require a signed-in user.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy(AdminOnly,
            policy => policy.RequireRole(nameof(UserRole.Admin)));

        options.AddPolicy(TeacherOrAdmin,
            policy => policy.RequireRole(nameof(UserRole.Teacher), nameof(UserRole.Admin)));

        options.AddPolicy(StudentOnly,
            policy => policy.RequireRole(nameof(UserRole.Student)));

        options.AddPolicy(CanPublishContent, policy => policy.RequireAssertion(context =>
            context.User.IsInRole(nameof(UserRole.Admin)) ||
            (context.User.IsInRole(nameof(UserRole.Teacher)) &&
             context.User.HasClaim(BelClaimTypes.Approved, bool.TrueString))));
    }
}
