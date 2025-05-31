namespace BlazorAuthExample.Auth;

public static class Policies
{
    /// <summary>
    ///     The default policy, which is used when simply [Authorize] is used.
    /// </summary>
    public const string DefaultPolicy = "DefaultPolicy";

    /// <summary>
    ///     The fallback policy, which is used when neither [Authorize] nor [AllowAnonymous] is used.
    /// </summary>
    public const string FallbackPolicy = "FallbackPolicy";
}
