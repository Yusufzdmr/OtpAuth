namespace OtpAuth.Infrastructure.Settings;

/// <summary>
/// appsettings.json -> "Sms" bölümünden bağlanır. ÖzTek Haberleşme SMS API bilgileri buraya girilir.
/// (POST ile 1-N gönderim: smsgonder1Npost.php — bkz. ÖzTek AP Dökümanı v7.1)
/// </summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>true: gerçek ÖzTek API'sine gönderir. false: kodu yalnızca loga yazar (geliştirme).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>POST gönderim adresi (1 mesaj - çok numara).</summary>
    public string BaseUrl { get; set; } = "http://www.ozteksms.com/panel/smsgonder1Npost.php";

    /// <summary>Kno — Kullanıcı kodunuz (Panel: Kullanıcı İşlemleri -> Kullanıcı Bilgileri).</summary>
    public string Kno { get; set; } = string.Empty;

    /// <summary>Kulad — Kullanıcı adınız.</summary>
    public string Kulad { get; set; } = string.Empty;

    /// <summary>Sifre — Şifreniz.</summary>
    public string Sifre { get; set; } = string.Empty;

    /// <summary>Gonderen — Onaylı originatör / gönderen başlık (3-11 karakter, Türkçe/özel karakter olamaz).</summary>
    public string Gonderen { get; set; } = string.Empty;

    /// <summary>Tur — Mesaj formatı: "Normal" (160 krk) veya "Turkce" (155 krk, Türkçe karakter destekli).</summary>
    public string Tur { get; set; } = "Normal";
}
