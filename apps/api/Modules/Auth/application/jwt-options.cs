namespace Hrm.Modules.Auth.Application;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SaigonRetail.Api";
    public string Audience { get; set; } = "SaigonRetail.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 1;
    public int RefreshTokenDays { get; set; } = 7;
}
