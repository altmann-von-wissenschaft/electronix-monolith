namespace Application;

/// <summary>
/// JWT signing and validation settings. Bound from configuration section "Jwt" with legacy defaults when values are missing.
/// </summary>
public sealed class JwtOptions
{
    public string SecretKey { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int AccessTokenLifetimeDays { get; set; } = 7;

    /// <summary>Original hard-coded defaults from AuthToken (used when config omits values).</summary>
    public static JwtOptions CreateWithLegacyFallbacks(JwtOptions? fromConfiguration)
    {
        var o = fromConfiguration ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(o.SecretKey))
            o.SecretKey = "7ntbLwQvepRN4X1Uv9o7m29DnPclkL7adynTm2ex";
        if (string.IsNullOrWhiteSpace(o.Issuer))
            o.Issuer = "electronix.api";
        if (string.IsNullOrWhiteSpace(o.Audience))
            o.Audience = "electronix.mobile";
        if (o.AccessTokenLifetimeDays <= 0)
            o.AccessTokenLifetimeDays = 7;
        return o;
    }
}
