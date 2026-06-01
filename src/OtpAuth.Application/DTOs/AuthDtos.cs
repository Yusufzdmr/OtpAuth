namespace OtpAuth.Application.DTOs;

/// <summary>Adım 1: Kullanıcı telefon numarasını gönderir, sisteme OTP üretmesini söyler.</summary>
public record RequestOtpRequest(string PhoneNumber);

/// <summary>Adım 2: Kullanıcı telefonuna gelen 6 haneli kodu doğrulamak için gönderir.</summary>
public record VerifyOtpRequest(string PhoneNumber, string Code);

/// <summary>OTP başarıyla doğrulandığında üretilen JWT oturum bilgisi.</summary>
public record AuthResponse(string Token, DateTime ExpiresAtUtc, string PhoneNumber);
