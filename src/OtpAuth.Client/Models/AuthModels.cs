namespace OtpAuth.Client.Models;

public record RequestOtpRequest(string PhoneNumber);

public record VerifyOtpRequest(string PhoneNumber, string Code);

public record AuthResponse(string Token, DateTime ExpiresAtUtc, string PhoneNumber);

/// <summary>Korumalı /me ucundan dönen kimlik bilgisi.</summary>
public record MeResponse(string? UserId, string? PhoneNumber);

/// <summary>API hata yanıtı: { "error": "..." }</summary>
public record ApiError(string? Error);
