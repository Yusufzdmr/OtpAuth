using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtpAuth.Application.Abstractions;
using OtpAuth.Application.DTOs;

namespace OtpAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Adım 1 — Telefon numarası için OTP üretir, kaydeder ve SMS ile gönderir.</summary>
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request, CancellationToken ct)
    {
        var result = await _authService.RequestOtpAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        // Güvenlik: numaranın kayıtlı olup olmadığını sızdırmamak için her zaman aynı yanıt.
        return Ok(new { message = "Kod gönderildi." });
    }

    /// <summary>Adım 2 — Kodu doğrular; geçerliyse JWT döner.</summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        var result = await _authService.VerifyOtpAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>Korumalı uç — yalnızca geçerli JWT ile erişilir. Token'daki kimliği döner.</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var phone = User.FindFirstValue(ClaimTypes.MobilePhone) ?? User.Identity?.Name;
        return Ok(new { userId, phoneNumber = phone });
    }
}
