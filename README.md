# OtpAuth — Blazor WASM (.NET 9) + Microsoft Identity ile Şifresiz OTP Girişi

Telefon numarası + 6 haneli tek kullanımlık kod (OTP) ile **şifresiz** giriş.
Onion (Clean) mimari, .NET 9 Web API, EF Core (MSSQL), Microsoft Identity, JWT ve MudBlazor.

## Mimari (Onion / Clean Architecture)

```
OtpAuth.sln
└── src/
    ├── OtpAuth.Domain          → Entity'ler (OtpCode). Hiçbir dış bağımlılık yok.
    ├── OtpAuth.Application      → Soyutlamalar (ISmsSender, IJwtTokenGenerator, IAuthService), DTO'lar, Result.
    ├── OtpAuth.Infrastructure   → EF Core DbContext, Identity, JWT üretimi, AuthService, SMS gönderici.
    ├── OtpAuth.Api              → .NET 9 Web API (AuthController), JWT auth, CORS, Swagger.
    └── OtpAuth.Client           → Blazor WASM + MudBlazor (Dark/Light, responsive), 2 layout, AuthState.
```

Bağımlılık yönü: `Api → Infrastructure → Application → Domain` (içe doğru).

## Giriş Akışı

1. Kullanıcı telefon numarasını girer → `POST /api/auth/request-otp`
2. Sunucu kriptografik 6 haneli kod üretir, **MSSQL**'e yazar ve **SMS** ile gönderir.
3. Kullanıcı kodu girer → `POST /api/auth/verify-otp`
4. Kod doğruysa **JWT** üretilir; client token'ı `localStorage`'a yazar ve oturum açılır.

---

## API Uçları

| Method | Yol | Koruma | Açıklama |
|---|---|---|---|
| POST | `/api/auth/request-otp` | Açık | Telefon için OTP üretir + SMS gönderir |
| POST | `/api/auth/verify-otp` | Açık | Kodu doğrular, JWT döner |
| GET  | `/api/auth/me` | 🔒 JWT | Token'daki kimliği döner (koruma örneği) |

Client tarafında `AuthorizationMessageHandler`, giden her isteğe JWT'yi otomatik ekler ve
sunucu **401** dönerse oturumu kapatıp login'e yönlendirir.

## Kurulum

### 1. Gereksinimler
- .NET 9 SDK (kurulu)
- MSSQL veritabanı — aşağıdaki seçeneklerden biri (Visual Studio 2022 kurarsanız LocalDB hazır gelir)

### 2. Veritabanı seçenekleri (birini seçin)

`appsettings.json` → `ConnectionStrings:DefaultConnection` değerini ortamınıza göre düzenleyin.

**a) LocalDB** (en hafif — yönetici olarak tek kurulum)
```powershell
# Yönetici PowerShell'de:
msiexec /i SqlLocalDB.msi /qn IACCEPTSQLLOCALDBLICENSETERMS=YES
```
Connection string:
```
Server=(localdb)\MSSQLLocalDB;Database=OtpAuthDb;Trusted_Connection=True;TrustServerCertificate=True
```

**b) SQL Server Express / SSMS** (varsayılan)
```
Server=.\SQLEXPRESS;Database=OtpAuthDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### 3. Veritabanını oluşturun (migration zaten hazır)
```powershell
dotnet tool install --global dotnet-ef   # bir kez
dotnet ef database update --project src/OtpAuth.Infrastructure --startup-project src/OtpAuth.Api
```

---

## Çalıştırma

İki projeyi aynı anda başlatın (VS2022'de: solution'a sağ tık → *Configure Startup Projects* → Multiple).

```powershell
# 1. terminal — API (https://localhost:7100, http://localhost:5100, Swagger: /swagger)
dotnet run --project src/OtpAuth.Api

# 2. terminal — Client (https://localhost:7200)
dotnet run --project src/OtpAuth.Client
```

Tarayıcı: `https://localhost:7200` → otomatik `/login` sayfasına yönlenir.

> **SMS dokümanı henüz yok:** `appsettings.json` → `Sms:Enabled = false` iken kod gerçek SMS yerine
> **API konsoluna** yazılır (`OTP üretildi => +90... : 123456`). Böylece akış uçtan uca test edilebilir.

---

## SMS Entegrasyonu (tek dosya)

GSM API dokümanı gelince **yalnızca** şu metodun içini doldurun:

`src/OtpAuth.Infrastructure/Sms/SmsSender.cs → SendViaProviderAsync(...)`

İçeride hazır `HttpClient`, `_options.BaseUrl`, `_options.ApiKey`, `_options.Sender` mevcut.
`appsettings.json` → `Sms` bölümünü doldurup `Enabled = true` yapın. Başka hiçbir yeri değiştirmeniz gerekmez.

---

## Güvenlik Notları (prod öncesi)
- `Jwt:SigningKey` değerini güçlü bir secret ile değiştirin (User Secrets / Key Vault).
- OTP politikası `appsettings.json` → `Otp` (süre, maksimum deneme) üzerinden ayarlanır.
- `request-otp` yanıtı numaranın kayıtlı olup olmadığını sızdırmaz (enumeration koruması).
- OTP brute-force'a karşı `MaxAttempts` ile kilitlenir.
