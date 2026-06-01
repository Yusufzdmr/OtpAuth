using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OtpAuth.Client;
using OtpAuth.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- MudBlazor ---
builder.Services.AddMudServices();

// --- Kimlik doğrulama (client-side) ---
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// --- API HttpClient (her isteğe otomatik JWT ekleyen handler ile) ---
// appsettings.json -> "ApiBaseUrl" ile override edilebilir; yoksa varsayılan dev portu.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7100/";
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddScoped<AuthApiClient>();

await builder.Build().RunAsync();
