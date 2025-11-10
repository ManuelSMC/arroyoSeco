namespace arroyoSeco.Infrastructure.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string Key { get; set; } = null!;
    public int ExpirationMinutes { get; set; } = 60;
}