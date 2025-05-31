using System.Runtime.Versioning;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BlazorAuthExample.Auth;

public interface ICurrentUser
{
    ClaimsPrincipal GetUser();
    JsonWebToken? GetToken();
    void SetUser(ClaimsPrincipal user);
    void SetToken(JsonWebToken token);
    void Clear();
    string? GetRefreshToken();
    void SetRefreshToken(string? token);
}

public class CurrentUser : ICurrentUser
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _refreshToken;
    private JsonWebToken? _token;

    public void Clear()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        _token = null;
        _refreshToken = null;
    }

    [UnsupportedOSPlatform("browser")]
    public string? GetRefreshToken()
    {
        return _refreshToken;
    }

    [UnsupportedOSPlatform("browser")]
    public JsonWebToken? GetToken()
    {
        return _token;
    }

    public ClaimsPrincipal GetUser()
    {
        return _currentUser;
    }

    [UnsupportedOSPlatform("browser")]
    public void SetRefreshToken(string? token)
    {
        _refreshToken = token;
    }

    [UnsupportedOSPlatform("browser")]
    public void SetToken(JsonWebToken token)
    {
        if (ReferenceEquals(_token, token) is false)
        {
            _token = token;
        }
    }

    public void SetUser(ClaimsPrincipal user)
    {
        if (ReferenceEquals(_currentUser, user) is false)
        {
            _currentUser = user;
        }
    }
}
