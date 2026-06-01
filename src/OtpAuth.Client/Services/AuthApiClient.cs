using System.Net.Http.Json;
using OtpAuth.Client.Models;

namespace OtpAuth.Client.Services;

/// <summary>Web API'nin /api/auth uçlarını çağıran istemci.</summary>
public class AuthApiClient
{
    private readonly HttpClient _http;
    private readonly JwtAuthenticationStateProvider _authState;

    public AuthApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    /// <summary>OTP isteği gönderir. Hata varsa mesajı döner, başarılıysa null.</summary>
    public async Task<string?> RequestOtpAsync(string phoneNumber)
    {
        var response = await _http.PostAsJsonAsync("api/auth/request-otp", new RequestOtpRequest(phoneNumber));
        if (response.IsSuccessStatusCode)
            return null;

        return await ReadErrorAsync(response);
    }

    /// <summary>Kodu doğrular. Başarılıysa oturum açar ve null döner; aksi halde hata mesajı.</summary>
    public async Task<string?> VerifyOtpAsync(string phoneNumber, string code)
    {
        var response = await _http.PostAsJsonAsync("api/auth/verify-otp", new VerifyOtpRequest(phoneNumber, code));
        if (!response.IsSuccessStatusCode)
            return await ReadErrorAsync(response);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
            return "Sunucudan geçersiz yanıt alındı.";

        await _authState.NotifyLoginAsync(auth.Token);
        return null;
    }

    /// <summary>Korumalı /me ucunu çağırır (JWT handler tarafından otomatik eklenir).</summary>
    public async Task<MeResponse?> GetMeAsync()
    {
        var response = await _http.GetAsync("api/auth/me");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MeResponse>();
    }

    public Task LogoutAsync() => _authState.NotifyLogoutAsync();

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            if (!string.IsNullOrWhiteSpace(error?.Error))
                return error.Error;
        }
        catch
        {
            // ignore — fall through to generic message
        }
        return "İşlem başarısız oldu. Lütfen tekrar deneyin.";
    }
}
