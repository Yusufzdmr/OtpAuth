using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpAuth.Application.Abstractions;
using OtpAuth.Infrastructure.Settings;

namespace OtpAuth.Infrastructure.Sms;

/// <summary>
/// ÖzTek Haberleşme SMS API entegrasyonu (POST ile 1-N gönderim — smsgonder1Npost.php).
///
/// SmsOptions.Enabled = false iken gerçek API'ye gitmez, yalnızca log'a yazılır;
/// bu sayede kimlik bilgileri girilmeden tüm OTP akışı uçtan uca test edilebilir.
///
/// Üst akış (OTP üretimi, doğrulama, JWT) bu sınıftan bağımsızdır.
/// </summary>
public class SmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(HttpClient httpClient, IOptions<SmsOptions> options, ILogger<SmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            // Geliştirme modu: gerçek SMS gitmez, içerik log'a düşer.
            _logger.LogWarning("[SMS DEVRE DIŞI] => {Phone} : {Message}", phoneNumber, message);
            return;
        }

        await SendViaProviderAsync(phoneNumber, message, cancellationToken);
    }

    /// <summary>
    /// ÖzTek POST gönderimi: "data" alanında XML olarak istek atılır.
    /// Cevap "1:" ile başlarsa başarılı, "2:" ile başlarsa hatalı kabul edilir.
    /// </summary>
    private async Task SendViaProviderAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        var numara = ToOztekNumber(phoneNumber);
        if (numara is null)
        {
            _logger.LogError("ÖzTek için geçersiz numara formatı, SMS gönderilemedi: {Phone}", phoneNumber);
            throw new InvalidOperationException($"ÖzTek için geçersiz numara: {phoneNumber}");
        }

        // ÖzTek AP Dökümanı'na göre XML yapısı (tek mesaj - çok numara, 1-N).
        var xml =
            "<sms>" +
            $"<kno>{Escape(_options.Kno)}</kno>" +
            $"<kulad>{Escape(_options.Kulad)}</kulad>" +
            $"<sifre>{Escape(_options.Sifre)}</sifre>" +
            $"<tur>{Escape(_options.Tur)}</tur>" +
            $"<gonderen>{Escape(_options.Gonderen)}</gonderen>" +
            $"<mesaj>{Escape(message)}</mesaj>" +
            $"<numaralar>{numara}</numaralar>" +
            "</sms>";

        // POST field'ı "data" olmak zorunda (form-urlencoded).
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("data", xml)
        });

        using var response = await _httpClient.PostAsync(_options.BaseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        // Olumlu durum belirteci "1:", olumsuz "2:" ile başlar.
        if (body.StartsWith("1:", StringComparison.Ordinal))
        {
            _logger.LogInformation("SMS gönderildi => {Phone} | ÖzTek cevap: {Resp}", phoneNumber, body);
            return;
        }

        _logger.LogError("SMS gönderilemedi => {Phone} | ÖzTek cevap: {Resp}", phoneNumber, body);
        throw new InvalidOperationException($"ÖzTek SMS hatası: {body}");
    }

    /// <summary>
    /// E.164 (+905xxxxxxxxx) veya yerel formatı ÖzTek'in beklediği 10 haneli (5xxxxxxxxx) biçime çevirir.
    /// Geçersizse null döner.
    /// </summary>
    private static string? ToOztekNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // +90 5XX XXX XX XX  -> 905XXXXXXXXX (12 hane)
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
            digits = digits[2..];
        // 0 5XX XXX XX XX -> 05XXXXXXXXX (11 hane)
        else if (digits.Length == 11 && digits.StartsWith("0", StringComparison.Ordinal))
            digits = digits[1..];

        return digits.Length == 10 && digits.StartsWith("5", StringComparison.Ordinal) ? digits : null;
    }

    /// <summary>XML değerlerini güvenli hale getirir (&amp;, &lt;, &gt;, tırnak vb.).</summary>
    private static string Escape(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
}
