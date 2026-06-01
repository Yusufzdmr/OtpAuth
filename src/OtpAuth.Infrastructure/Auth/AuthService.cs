using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpAuth.Application.Abstractions;
using OtpAuth.Application.Common;
using OtpAuth.Application.DTOs;
using OtpAuth.Domain.Entities;
using OtpAuth.Infrastructure.Identity;
using OtpAuth.Infrastructure.Persistence;
using OtpAuth.Infrastructure.Settings;

namespace OtpAuth.Infrastructure.Auth;

/// <summary>Şifresiz OTP giriş akışının iş mantığı.</summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ISmsSender _sms;
    private readonly OtpOptions _otpOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwt,
        ISmsSender sms,
        IOptions<OtpOptions> otpOptions,
        ILogger<AuthService> logger)
    {
        _db = db;
        _userManager = userManager;
        _jwt = jwt;
        _sms = sms;
        _otpOptions = otpOptions.Value;
        _logger = logger;
    }

    public async Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        if (phone is null)
            return Result.Fail("Geçersiz telefon numarası.");

        var now = DateTime.UtcNow;

        // Aynı numaraya ait önceki kullanılmamış kodları geçersiz kıl (tek aktif kod kalsın).
        var activeCodes = await _db.OtpCodes
            .Where(o => o.PhoneNumber == phone && !o.IsUsed)
            .ToListAsync(cancellationToken);
        foreach (var c in activeCodes)
            c.IsUsed = true;

        var code = GenerateSixDigitCode();
        var otp = new OtpCode
        {
            PhoneNumber = phone,
            Code = code,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(_otpOptions.ExpiryMinutes),
            IsUsed = false,
            AttemptCount = 0
        };

        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync(cancellationToken);

        if (_otpOptions.LogCodeToConsole)
            _logger.LogInformation("OTP üretildi => {Phone} : {Code} (geçerlilik {Min} dk)",
                phone, code, _otpOptions.ExpiryMinutes);

        var message = $"Giris kodunuz: {code}. Kod {_otpOptions.ExpiryMinutes} dakika gecerlidir.";
        await _sms.SendAsync(phone, message, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        if (phone is null)
            return Result<AuthResponse>.Fail("Geçersiz telefon numarası.");

        var now = DateTime.UtcNow;

        var otp = await _db.OtpCodes
            .Where(o => o.PhoneNumber == phone && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
            return Result<AuthResponse>.Fail("Aktif bir kod bulunamadı. Lütfen yeni kod isteyin.");

        if (otp.IsExpired(now))
            return Result<AuthResponse>.Fail("Kodun süresi doldu. Lütfen yeni kod isteyin.");

        if (otp.AttemptCount >= _otpOptions.MaxAttempts)
        {
            otp.IsUsed = true;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Fail("Çok fazla hatalı deneme. Lütfen yeni kod isteyin.");
        }

        if (otp.Code != request.Code?.Trim())
        {
            otp.AttemptCount++;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Fail("Kod hatalı.");
        }

        // Başarılı: kodu tek kullanımlık olarak işaretle.
        otp.IsUsed = true;
        await _db.SaveChangesAsync(cancellationToken);

        // Kullanıcı yoksa oluştur (passwordless — sadece telefonla tanımlı).
        var user = await EnsureUserAsync(phone);

        var (token, expiresAtUtc) = _jwt.Generate(user.Id, phone);
        return Result<AuthResponse>.Success(new AuthResponse(token, expiresAtUtc, phone));
    }

    private async Task<ApplicationUser> EnsureUserAsync(string phone)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        if (user is not null)
            return user;

        user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Kullanıcı oluşturulamadı: {errors}");
        }

        return user;
    }

    /// <summary>Kriptografik olarak güvenli 6 haneli kod üretir (000000–999999).</summary>
    private static string GenerateSixDigitCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    /// <summary>
    /// Basit telefon normalizasyonu: boşluk/tire temizler, Türkiye için E.164'e (+90...) çevirir.
    /// İhtiyaca göre libphonenumber gibi bir kütüphane ile güçlendirilebilir.
    /// </summary>
    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (raw.TrimStart().StartsWith('+'))
            return "+" + digits;

        // 0XXXXXXXXXX (11 hane) -> +90XXXXXXXXXX
        if (digits.Length == 11 && digits.StartsWith('0'))
            return "+90" + digits[1..];

        // 5XXXXXXXXX (10 hane) -> +90XXXXXXXXXX
        if (digits.Length == 10)
            return "+90" + digits;

        return digits.Length >= 10 ? "+" + digits : null;
    }
}
