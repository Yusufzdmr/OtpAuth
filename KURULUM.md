# Kurulum Kılavuzu (Adım Adım)

Bu belge, projeyi sıfırdan bir makinede çalıştırmak için gereken tüm adımları içerir.

---

## 0. Gereksinimler (özet)

| Araç | Amaç | Zorunlu mu? |
|---|---|---|
| .NET 9 SDK | Derleme / çalıştırma | ✅ Evet |
| Visual Studio 2022 (17.12+) | Teslim/geliştirme ortamı | ✅ (CLI ile de olur) |
| SQL Server (Express/LocalDB) | Veritabanı | ✅ Evet |
| SSMS | DB yönetimi (opsiyonel) | ➖ İsteğe bağlı |

---

## 1. .NET 9 SDK Kurulumu

1. https://dotnet.microsoft.com/download/dotnet/9.0 adresine git.
2. **SDK x64** (Windows) indir ve kur.
3. Doğrula (yeni bir terminal aç):
   ```powershell
   dotnet --version      # 9.0.x görmelisin
   ```

> winget ile: `winget install Microsoft.DotNet.SDK.9`

---

## 2. Visual Studio 2022 Kurulumu

1. https://visualstudio.microsoft.com/downloads/ → **Community** (ücretsiz) indir.
2. Kurulum sihirbazında **Workloads** sekmesinden işaretle:
   - ☑️ **ASP.NET and web development**
3. Sağ paneldeki **Installation details** altında **.NET 9** bileşeninin seçili olduğundan emin ol.
4. **Install** → kurulum bitince (gerekirse) yeniden başlat.

> winget ile (yönetici terminal):
> ```powershell
> winget install Microsoft.VisualStudio.2022.Community --override "--add Microsoft.VisualStudio.Workload.NetWeb --includeRecommended --passive --norestart"
> ```

---

## 3. Veritabanı Kurulumu (bir seçenek seç)

### Seçenek A — LocalDB (en hafif, önerilen)
VS2022 kurduysan LocalDB **zaten gelir**. Yoksa LocalDB MSI'sini yönetici olarak kur:
```powershell
msiexec /i SqlLocalDB.msi /qn IACCEPTSQLLOCALDBLICENSETERMS=YES
```
Connection string:
```
Server=(localdb)\MSSQLLocalDB;Database=OtpAuthDb;Trusted_Connection=True;TrustServerCertificate=True
```

### Seçenek B — SQL Server Express
1. https://www.microsoft.com/download/details.aspx?id=104781 → indir ve kur (Basic).
2. (Opsiyonel) SSMS kur: https://aka.ms/ssmsfullsetup
3. Connection string:
```
Server=.\SQLEXPRESS;Database=OtpAuthDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

---

## 4. Projeyi Açma

- **Visual Studio:** *Open a project or solution* → `OtpAuth.sln`
- **VS Code / CLI:** klasörü aç, terminalde `dotnet restore`

---

## 5. Connection String'i Ayarla

`src/OtpAuth.Api/appsettings.json` → `ConnectionStrings:DefaultConnection` değerini
3. adımda seçtiğin string ile değiştir.

Aynı dosyada gerekirse:
- `Jwt:SigningKey` → güçlü, en az 32 karakter bir değer ver.
- `Otp` → kod süresi / maksimum deneme.

---

## 6. Veritabanını Oluştur (migration hazır)

EF aracını bir kez kur:
```powershell
dotnet tool install --global dotnet-ef
```
Veritabanını oluştur:
```powershell
# Çözüm kök klasöründe:
dotnet ef database update --project src/OtpAuth.Infrastructure --startup-project src/OtpAuth.Api
```
> Visual Studio'da alternatif: **Package Manager Console** → `Update-Database`
> (Default project: `OtpAuth.Infrastructure`, Startup project: `OtpAuth.Api`)

---

## 7. SMS Ayarı (opsiyonel — doküman gelince)

`src/OtpAuth.Api/appsettings.json` → `Sms` bölümü:
- Doküman **yoksa**: `Enabled: false` bırak → kod API konsoluna yazılır (test edilebilir).
- Doküman **varsa**: `BaseUrl`, `ApiKey`, `Sender` doldur, `Enabled: true` yap ve
  `src/OtpAuth.Infrastructure/Sms/SmsSender.cs → SendViaProviderAsync` içine gerçek çağrıyı ekle.

---

## 8. Çalıştırma

### Visual Studio (önerilen)
1. Solution'a sağ tık → **Configure Startup Projects** → **Multiple startup projects**.
2. `OtpAuth.Api` ve `OtpAuth.Client` için **Action = Start** seç → OK.
3. **F5**.

### CLI (iki ayrı terminal)
```powershell
# Terminal 1 — API  (https://localhost:7100, Swagger: /swagger)
dotnet run --project src/OtpAuth.Api

# Terminal 2 — Client (https://localhost:7200)
dotnet run --project src/OtpAuth.Client
```
Tarayıcı: `https://localhost:7200` → otomatik `/login` ekranına gider.

---

## 9. Test Akışı
1. Telefon numarası gir (örn. `05551112233`) → **Kod Gönder**.
2. Kod, `Sms:Enabled=false` iken **API konsolunda** görünür (`OTP üretildi => ... : 123456`).
3. Kodu gir → **Giriş Yap** → korumalı ana sayfaya yönlenirsin.

---

## 10. Sorun Giderme

| Hata | Çözüm |
|---|---|
| `dotnet` tanınmıyor | Yeni terminal aç / .NET 9 SDK kur |
| DB'ye bağlanılamıyor | Connection string'i ve SQL servisinin çalıştığını kontrol et |
| CORS hatası (client→API) | `appsettings.json → AllowedOrigins` client adresini içermeli (7200/5200) |
| HTTPS sertifika uyarısı | `dotnet dev-certs https --trust` çalıştır |
| 401 / token çalışmıyor | `Jwt` ayarları API ve token üretiminde aynı olmalı (tek kaynak: appsettings) |
| `request-otp` 500 veriyor | Genelde DB yok/migration uygulanmamış → 6. adımı yap |
