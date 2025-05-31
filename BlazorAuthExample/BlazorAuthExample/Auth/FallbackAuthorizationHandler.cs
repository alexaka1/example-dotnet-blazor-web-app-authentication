using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace BlazorAuthExample.Auth;

// A simple marker requirement for our custom fallback logic
public class EvaluateDefaultPolicyRequirement : IAuthorizationRequirement;

/// <summary>
///     A special authorization handler that should be used for the fallback policy ONLY. It skips certain static assets
///     due to a Blazor bug.
/// </summary>
/// <remarks>https://github.com/dotnet/aspnetcore/issues/51836</remarks>
/// <param name="policyProvider"></param>
/// <param name="serviceProvider"></param>
/// <param name="httpContextAccessor"></param>
public partial class FallbackAuthorizationHandler(
    IAuthorizationPolicyProvider policyProvider,
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<FallbackAuthorizationHandler> logger)
    : AuthorizationHandler<EvaluateDefaultPolicyRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EvaluateDefaultPolicyRequirement requirement)
    {
        // Try to get HttpContext (usually the resource in endpoint routing)
        var httpContext = context.Resource as HttpContext ?? httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            LogHttpContextNull();
            // Cannot determine request details, let other handlers decide or fail.
            return;
        }

        var request = httpContext.Request;
        string? path = request.Path.Value;

        // --- Static Asset Check ---
        bool isAllowedStaticAsset = false;
        // exclude certain assets https://github.com/dotnet/aspnetcore/issues/51836
        if (request.Method == HttpMethods.Get)
        {
            if (path == "/_framework/blazor.web.js" ||
                path == "/favicon.png" ||
                (path != null && StaticAssetExcludes.ResourceCollectionJs().IsMatch(path)))
            {
                isAllowedStaticAsset = true;
            }
        }

        if (isAllowedStaticAsset)
        {
            // If it's an allowed static asset, this requirement is met.
            context.Succeed(requirement);
            LogStaticAssetAllowed();
            return;
        }

        // --- Evaluate Default Policy ---
        // If it's not an allowed static asset, evaluate the actual default policy.
        // Retrieve the default policy instance using its registered name.
        var defaultPolicy = await policyProvider.GetPolicyAsync(Policies.DefaultPolicy);
        Debug.Assert(defaultPolicy != null);

        // Use IAuthorizationService to evaluate the default policy's requirements
        // against the current user and resource.
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, // The current user principal
            context.Resource, // The resource being accessed (often HttpContext)
            defaultPolicy // The policy to evaluate
        );

        if (authorizationResult.Succeeded)
        {
            // If the default policy evaluation succeeded, this requirement is met.
            context.Succeed(requirement);
            LogDefaultPolicySuccess();
        }
        // If authorizationResult failed, we do nothing, and the requirement remains unmet.
        // The authorization middleware will handle the overall failure.
    }

    [LoggerMessage(LogLevel.Trace, Message = "Static asset allowed", EventName = "StaticAssetAllowed")]
    private partial void LogStaticAssetAllowed();

    [LoggerMessage(LogLevel.Trace, Message = "Default policy success", EventName = "DefaultPolicySuccess")]
    private partial void LogDefaultPolicySuccess();

    [LoggerMessage(LogLevel.Trace, Message = "HttpContext is null", EventName = "HttpContextNull")]
    private partial void LogHttpContextNull();
}

public static partial class StaticAssetExcludes
{
    [GeneratedRegex(@"^/_framework/resource-collection(?:\.[^.]+)?\.js$", RegexOptions.Compiled)]
    public static partial Regex ResourceCollectionJs();
}
