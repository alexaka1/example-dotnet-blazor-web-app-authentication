using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BlazorAuthExample.Auth;

public class CustomAuthenticationHandler(
    IOptionsMonitor<CustomAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    JwtTokenProvider jwtTokenProvider,
    AuthenticationStateProvider authenticationStateProvider,
    JwtGenerator jwtGenerator) : AuthenticationHandler<CustomAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = jwtTokenProvider.GetTokenFromClient();
        string? refreshToken = jwtTokenProvider.GetRefreshTokenFromClient();

        if (token is null)
        {
            return AuthenticateResult.NoResult();
        }

        var validationResult = await ValidateTokenAsync(token, Context.RequestAborted);
        if (validationResult.Error)
        {
            return AuthenticateResult.Fail(validationResult.ErrorMessage);
        }

        var identity = CreateClaimsIdentity(token);

        var claimsPrincipal = new ClaimsPrincipal(identity);
        Debug.Assert(authenticationStateProvider is CustomAuthenticationStateProvider);
        ((CustomAuthenticationStateProvider)authenticationStateProvider).AuthenticateUser(claimsPrincipal, token,
            refreshToken);


        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal,
            CustomAuthenticationOptions.SchemeName));
    }

    internal static ClaimsIdentity CreateClaimsIdentity(JsonWebToken token)
    {
        return new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Sid, "1234567890"),
            new Claim(ClaimTypes.Name, "Bob Bobertson"),
            ..token.Claims.Where(c => c.Type == ClaimTypes.Role)
                .Select(c => new Claim(ClaimTypes.Role, c.Value)),
            ..token.Claims.Where(c => c.Type == "Scope")
                .Select(c => new Claim("Scope", c.Value)),
        ], CustomAuthenticationOptions.SchemeName);
    }

    private async Task<ValidationResult> ValidateTokenAsync(JsonWebToken token,
        CancellationToken cancellationToken = default)
    {
        var result = await jwtGenerator.ValidateJwt(token, cancellationToken);
        return !result.IsValid ? new ValidationResult(true, result.Exception.Message) : ValidationResult.Success();
    }
}

public class CustomAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "MyScheme";

    public const string CookieNameAuthToken = "AuthToken";
    public const string CookieNameAuthRefreshToken = "RefreshToken";
}

internal record ValidationResult(
    [property: MemberNotNullWhen(true, nameof(ValidationResult.ErrorMessage))]
    bool Error = false,
    string? ErrorMessage = null)
{
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }
}
