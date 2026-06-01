using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace OtpAuth.Client.Services;

/// <summary>
/// Giden tüm API isteklerine localStorage'daki JWT'yi "Authorization: Bearer ..." olarak ekler.
/// Sunucu 401 dönerse oturumu sonlandırıp kullanıcıyı login sayfasına yönlendirir.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;
    private readonly JwtAuthenticationStateProvider _authState;
    private readonly NavigationManager _navigation;

    public AuthorizationMessageHandler(
        TokenStore tokenStore,
        JwtAuthenticationStateProvider authState,
        NavigationManager navigation)
    {
        _tokenStore = tokenStore;
        _authState = authState;
        _navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token süresi dolmuş ya da geçersiz → temizle ve giriş ekranına dön.
            await _authState.NotifyLogoutAsync();
            _navigation.NavigateTo("login", forceLoad: false);
        }

        return response;
    }
}
