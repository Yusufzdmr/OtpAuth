using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace OtpAuth.Client.Services;

/// <summary>
/// localStorage'daki JWT'yi okuyup uygulamanın kimlik durumunu (AuthenticationState) üretir.
/// Süresi dolmuş token'ı geçersiz sayar. AuthorizeView / [Authorize] bunu kullanır.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly TokenStore _tokenStore;
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthenticationStateProvider(TokenStore tokenStore) => _tokenStore = tokenStore;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token).ToList();

        // Süre kontrolü (exp claim, unix saniye).
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (exp is not null && long.TryParse(exp, out var expSeconds))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (expiry <= DateTimeOffset.UtcNow)
            {
                await _tokenStore.RemoveTokenAsync();
                return Anonymous;
            }
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Başarılı giriş sonrası çağrılır — token'ı saklar ve UI'a yeni durumu bildirir.</summary>
    public async Task NotifyLoginAsync(string token)
    {
        await _tokenStore.SetTokenAsync(token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>Çıkış — token'ı siler ve UI'a anonim durumu bildirir.</summary>
    public async Task NotifyLogoutAsync()
    {
        await _tokenStore.RemoveTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return [];

        var payload = parts[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        if (keyValuePairs is null)
            return [];

        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
