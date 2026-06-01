namespace OtpAuth.Infrastructure.Settings;

/// <summary>appsettings.json -> "Jwt" bölümünden bağlanır.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;

    /// <summary>HMAC-SHA256 imzalama anahtarı. En az 32 karakter olmalıdır. Prod'da secret store'da tutun.</summary>
    public string SigningKey { get; set; } = default!;

    public int ExpiryMinutes { get; set; } = 60;
}
