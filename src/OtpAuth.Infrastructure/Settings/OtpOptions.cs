namespace OtpAuth.Infrastructure.Settings;

/// <summary>appsettings.json -> "Otp" bölümünden bağlanır. OTP politika ayarları.</summary>
public class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>Kodun geçerlilik süresi (dakika).</summary>
    public int ExpiryMinutes { get; set; } = 3;

    /// <summary>Bir kod için izin verilen maksimum yanlış deneme sayısı.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Geliştirme ortamında kodu SMS yerine loga yazıp test etmeyi kolaylaştırır.</summary>
    public bool LogCodeToConsole { get; set; } = true;
}
