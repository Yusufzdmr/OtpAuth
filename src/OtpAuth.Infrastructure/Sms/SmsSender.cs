using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpAuth.Application.Abstractions;
using OtpAuth.Infrastructure.Settings;

namespace OtpAuth.Infrastructure.Sms;

/// <summary>
/// SMS gönderim implementasyonu.
///
/// >>> ENTEGRASYON NOKTASI <<<
/// Müşterinin GSM API dokümanı gelince SADECE <see cref="SendViaProviderAsync"/> metodunun
/// içini doldurmanız yeterli. Üst akış (OTP üretimi, doğrulama, JWT) hiç değişmez.
///
/// SmsOptions.Enabled = false iken kod gerçek API'ye gitmez, yalnızca log'a yazılır;
/// bu sayede doküman gelmeden tüm akış uçtan uca test edilebilir.
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
    /// TODO: GSM API ENTEGRASYONU — burayı doldurun.
    /// Örnek iskelet (gerçek dokümana göre uyarlayın):
    /// </summary>
    private async Task SendViaProviderAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        // ---------------------------------------------------------------------
        // ÖRNEK / İSKELET — müşterinin dokümanına göre değiştirin:
        //
        // var payload = new
        // {
        //     apiKey  = _options.ApiKey,
        //     sender  = _options.Sender,
        //     to      = phoneNumber,
        //     message = message
        // };
        //
        // using var response = await _httpClient.PostAsJsonAsync(
        //     $"{_options.BaseUrl}/send", payload, cancellationToken);
        // response.EnsureSuccessStatusCode();
        // ---------------------------------------------------------------------

        _logger.LogInformation("SMS gönderiliyor => {Phone}", phoneNumber);
        await Task.CompletedTask; // <-- gerçek çağrı eklenince kaldırın
        throw new NotImplementedException(
            "GSM API entegrasyonu SmsSender.SendViaProviderAsync içine eklenmeli. " +
            "Geliştirme için appsettings.json -> Sms:Enabled = false bırakın.");
    }
}
