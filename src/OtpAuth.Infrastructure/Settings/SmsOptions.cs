namespace OtpAuth.Infrastructure.Settings;

/// <summary>
/// appsettings.json -> "Sms" bölümünden bağlanır. Müşterinin GSM API bilgileri buraya girilecek.
/// </summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>true: gerçek GSM API'ye gönderir. false: kodu yalnızca loga yazar (geliştirme).</summary>
    public bool Enabled { get; set; } = false;

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gönderen başlık / originator (örn. firma adı).</summary>
    public string Sender { get; set; } = string.Empty;
}
