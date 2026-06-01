using OtpAuth.Application.Common;
using OtpAuth.Application.DTOs;

namespace OtpAuth.Application.Abstractions;

/// <summary>Şifresiz (passwordless) OTP giriş akışının iş mantığı sözleşmesi.</summary>
public interface IAuthService
{
    /// <summary>OTP üretir, MSSQL'e yazar ve SMS ile gönderir.</summary>
    Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gelen kodu doğrular; geçerliyse JWT üretip oturum bilgisini döner.</summary>
    Task<Result<AuthResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
}
