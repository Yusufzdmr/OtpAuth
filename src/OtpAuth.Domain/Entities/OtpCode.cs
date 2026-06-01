namespace OtpAuth.Domain.Entities;

/// <summary>
/// Bir telefon numarasına gönderilen tek kullanımlık doğrulama kodunu (OTP) temsil eder.
/// MSSQL'de "OtpCodes" tablosunda saklanır.
/// </summary>
public class OtpCode
{
    public int Id { get; set; }

    /// <summary>E.164 formatında normalize edilmiş telefon numarası (örn. +905551112233).</summary>
    public string PhoneNumber { get; set; } = default!;

    /// <summary>6 haneli doğrulama kodu.</summary>
    public string Code { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Kod başarıyla doğrulandı mı? (tek kullanımlık olması için)</summary>
    public bool IsUsed { get; set; }

    /// <summary>Yapılan yanlış deneme sayısı (brute-force koruması için).</summary>
    public int AttemptCount { get; set; }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>Kod hâlâ kullanılabilir mi (kullanılmamış ve süresi dolmamış).</summary>
    public bool IsUsable(DateTime nowUtc) => !IsUsed && !IsExpired(nowUtc);
}
