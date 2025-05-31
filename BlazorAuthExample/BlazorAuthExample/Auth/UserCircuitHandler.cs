using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BlazorAuthExample.Auth;

internal sealed partial class UserCircuitHandler(
    AuthenticationStateProvider authenticationStateProvider,
    ICurrentUser userService,
    JwtTokenProvider tokenProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<UserCircuitHandler> logger)
    : CircuitHandler, IDisposable
{
    public void Dispose()
    {
        authenticationStateProvider.AuthenticationStateChanged -=
            AuthenticationChanged;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        // sets up an event handler for authentication state changes DURING the circuit
        authenticationStateProvider.AuthenticationStateChanged +=
            AuthenticationChanged;

        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    private void AuthenticationChanged(Task<AuthenticationState> task)
    {
        _ = UpdateAuthentication(task);
    }

    private async Task UpdateAuthentication(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            userService.SetUser(state.User);
            if (tokenProvider.GetTokenFromClient() is { } token)
            {
                userService.SetToken(token);
            }

            userService.SetRefreshToken(tokenProvider.GetRefreshTokenFromClient());
        }
        catch (Exception e)
        {
            LogResolveUserError(e);
        }
    }

    public override Task OnConnectionUpAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        if (authenticationStateProvider is not CustomAuthenticationStateProvider zaWinAuthenticationStateProvider
            || httpContextAccessor.HttpContext?.User is not { Identity.IsAuthenticated: true } newUser)
        {
            return Task.CompletedTask;
        }

        // on reconnect we explicitly update the authentication state
        // this is because since the circuit was opened, the user may have lost authentication, so we can't
        // use the old authentication state
        var token = tokenProvider.GetTokenFromClient();
        string? refreshToken = tokenProvider.GetRefreshTokenFromClient();
        zaWinAuthenticationStateProvider.AuthenticateUser(newUser, token, refreshToken);

        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            var ct = UserEnricher.EnrichSerilogWithUser(userService.GetUser());
            await next(context);
            foreach (var logContext in ct)
            {
                logContext.Dispose();
            }
        };
    }

    [LoggerMessage(LogLevel.Trace, "Exception while resolving user", EventName = "ResolveUserError")]
    private partial void LogResolveUserError(Exception exception);
}
