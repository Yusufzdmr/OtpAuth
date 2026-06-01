using Microsoft.JSInterop;

namespace OtpAuth.Client.Services;

/// <summary>
/// JWT'yi tarayıcının localStorage'ında saklar (harici paket olmadan, saf JS interop ile).
/// </summary>
public class TokenStore
{
    private const string Key = "otpauth_token";
    private readonly IJSRuntime _js;

    public TokenStore(IJSRuntime js) => _js = js;

    public ValueTask<string?> GetTokenAsync() =>
        _js.InvokeAsync<string?>("localStorage.getItem", Key);

    public ValueTask SetTokenAsync(string token) =>
        _js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public ValueTask RemoveTokenAsync() =>
        _js.InvokeVoidAsync("localStorage.removeItem", Key);
}
