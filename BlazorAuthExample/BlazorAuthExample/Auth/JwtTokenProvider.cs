using Microsoft.IdentityModel.JsonWebTokens;

namespace BlazorAuthExample.Auth;

public class JwtTokenProvider(
    IHttpContextAccessor httpContextAccessor)
{
    public JsonWebToken? GetTokenFromClient()
    {
        string? cookieValue = httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c =>
            c.Key == CustomAuthenticationOptions.CookieNameAuthToken
            && string.IsNullOrEmpty(c.Value) is false).Value;

        if (string.IsNullOrEmpty(cookieValue))
        {
            return null;
        }


        string token = cookieValue;
        return new JsonWebToken(token);
    }

    public void PersistTokenToClient(JsonWebToken token)
    {
        string? cookieValue = token.EncodedToken;
        httpContextAccessor.HttpContext?.Response.Cookies.Append(CustomAuthenticationOptions.CookieNameAuthToken,
            cookieValue, new CookieOptions
            {
                IsEssential = true,
                HttpOnly = true,
                Secure = true,
                Expires = new DateTimeOffset(token.ValidTo),
                SameSite = SameSiteMode.Strict,
            });
    }

    public string? GetRefreshTokenFromClient()
    {
        string? cookieValue = httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c =>
            c.Key == CustomAuthenticationOptions.CookieNameAuthRefreshToken
            && string.IsNullOrEmpty(c.Value) is false).Value;

        if (string.IsNullOrEmpty(cookieValue))
        {
            return null;
        }


        string token = cookieValue;
        return token;
    }

    public void PersistRefreshTokenToClient(string token)
    {
        string cookieValue = token;
        httpContextAccessor.HttpContext?.Response.Cookies.Append(
            CustomAuthenticationOptions.CookieNameAuthRefreshToken,
            cookieValue, new CookieOptions
            {
                IsEssential = true,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
            });
    }
}
