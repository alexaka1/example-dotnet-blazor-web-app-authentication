using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BlazorAuthExample.Auth;

public class JwtGenerator(TimeProvider timeProvider, IOptionsMonitor<JwtOptions> options)
{
    public string GenerateJwtToken(params IEnumerable<Claim> claims)
    {
        string secretKey = options.CurrentValue.Secret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        // Token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = new DateTime(timeProvider.GetUtcNow().AddHours(1).Ticks, DateTimeKind.Utc),
            SigningCredentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256),
            Issuer = "localhost",
            Audience = "localhost",
        };

        // Create token
        var tokenHandler = new JsonWebTokenHandler();
        string? token = tokenHandler.CreateToken(tokenDescriptor);

        // Return serialized token
        return token;
    }

    public async Task<TokenValidationResult> ValidateJwt(JsonWebToken token,
        CancellationToken cancellationToken = default)
    {
        string secretKey = options.CurrentValue.Secret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidIssuer = "localhost",
            ValidAudience = "localhost",
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tokenHandler = new JsonWebTokenHandler();
            return await tokenHandler.ValidateTokenAsync(token.EncodedToken, validationParameters);
        }
        catch (Exception e)
        {
            return new TokenValidationResult()
            {
                IsValid = false,
                Exception = e,
            };
        }
    }
}
