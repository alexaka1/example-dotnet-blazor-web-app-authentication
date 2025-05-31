namespace BlazorAuthExample.Auth;

public class JwtOptions
{
    public string Secret { get; set; } = Guid.NewGuid().ToString();
    public const string SectionName = "Jwt";
}
