namespace OtpAuth.Application.Abstractions;

/// <summary>JWT üretim soyutlaması. Implementasyonu Infrastructure katmanındadır.</summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) Generate(string userId, string phoneNumber);
}
