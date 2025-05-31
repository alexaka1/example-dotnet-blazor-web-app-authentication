using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BlazorAuthExample.Auth;

public partial class CustomAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    ICurrentUser currentUser, JwtGenerator jwtGenerator) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    private readonly ILogger<CustomAuthenticationStateProvider> _logger =
        loggerFactory.CreateLogger<CustomAuthenticationStateProvider>();

    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(10);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.GetToken() is not { } token)
            {
                return false;
            }
            var result = await jwtGenerator.ValidateJwt(token, cancellationToken);

            return result.IsValid;
        }
        catch (Exception e)
        {
            LogJwtTokenValidationError(e);
        }

        LogUserRevalidationFailed();
        return false;
    }

    public void AuthenticateUser(JsonWebToken token, string? refreshToken = null)
    {
        // set the current user and token in the scoped service, and the internal state
        var authenticationState = CreateAuthenticationState(token);
        currentUser.SetUser(authenticationState.User);
        currentUser.SetToken(token);
        currentUser.SetRefreshToken(refreshToken);
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
    }

    public void AuthenticateUser(ClaimsPrincipal user, JsonWebToken? token = null, string? refreshToken = null)
    {
        // set the current user and token in the scoped service, and the internal state
        var authenticationState = new AuthenticationState(user);
        currentUser.SetUser(authenticationState.User);
        if (token != null)
        {
            currentUser.SetToken(token);
        }

        currentUser.SetRefreshToken(refreshToken);

        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
    }

    public void LogoutUser()
    {
        // clear the current user and internal state
        var authenticationState = CreateAnonymousIdentity();
        currentUser.Clear();
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
    }

    private static AuthenticationState CreateAuthenticationState(JsonWebToken token)
    {
        var identity = CustomAuthenticationHandler.CreateClaimsIdentity(token);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static AuthenticationState CreateAnonymousIdentity()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    [LoggerMessage(LogLevel.Debug, "JWT token validation failed", EventName = "JwtTokenValidationError")]
    private partial void LogJwtTokenValidationError(Exception exception);

    [LoggerMessage(LogLevel.Trace, "User revalidation failed", EventName = "UserRevalidationFailed")]
    private partial void LogUserRevalidationFailed();
}
