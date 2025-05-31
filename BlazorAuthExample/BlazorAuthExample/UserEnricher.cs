using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using BlazorAuthExample.Auth;

namespace BlazorAuthExample;

public static class UserEnricher
{
    [SuppressMessage("ReSharper", "InconsistentContextLogPropertyNaming")]
    public static IEnumerable<IDisposable> EnrichSerilogWithUser(ClaimsPrincipal user)
    {
        var properties = new List<IDisposable>(3);

        return properties;
    }

    // public static void EnrichWithUser(this IDiagnosticContext context, ClaimsPrincipal user)
    // {
    //
    // }
}

public class UserEnrichMiddleware(ICurrentUser userService) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var contexts = UserEnricher.EnrichSerilogWithUser(userService.GetUser());

        await next(context);

        foreach (var logContext in contexts)
        {
            logContext.Dispose();
        }
    }
}
