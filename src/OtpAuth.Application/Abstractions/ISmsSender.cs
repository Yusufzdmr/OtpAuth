namespace OtpAuth.Application.Abstractions;

/// <summary>
/// SMS/GSM sağlayıcısı soyutlaması. Gerçek entegrasyon Infrastructure katmanındaki
/// <c>SmsSender</c> içinde yapılır — müşterinin GSM API dokümanı oraya bağlanacaktır.
/// </summary>
public interface ISmsSender
{
    /// <param name="phoneNumber">E.164 formatında hedef numara (örn. +905551112233).</param>
    /// <param name="message">Gönderilecek mesaj metni (OTP kodunu içerir).</param>
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
