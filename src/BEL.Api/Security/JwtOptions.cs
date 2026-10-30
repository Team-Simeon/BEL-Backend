namespace BEL.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "BEL.Api";

    public string Audience { get; set; } = "BEL.Client";

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
