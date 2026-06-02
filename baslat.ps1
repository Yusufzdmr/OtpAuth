# ============================================================
#  OtpAuth - Tek tikla baslatma
#  API (https://localhost:7100) + Client (https://localhost:7200)
#  ikisini de ayri pencerelerde dogru profille baslatir.
#
#  Kullanim:  PowerShell'de bu klasorde ->  .\baslat.ps1
#  (Calismazsa once:  Set-ExecutionPolicy -Scope Process Bypass)
# ============================================================

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "HTTPS gelistirme sertifikasi kontrol ediliyor..." -ForegroundColor Cyan
dotnet dev-certs https --trust | Out-Null

Write-Host "API baslatiliyor  -> https://localhost:7100" -ForegroundColor Green
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$root'; dotnet run --project src/OtpAuth.Api --launch-profile https"
)

# API'nin ayaga kalkmasi icin kisa bir bekleme
Start-Sleep -Seconds 4

Write-Host "Client baslatiliyor -> https://localhost:7200" -ForegroundColor Green
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location '$root'; dotnet run --project src/OtpAuth.Client --launch-profile https"
)

Write-Host ""
Write-Host "Hazir. Tarayicida acilacak: https://localhost:7200" -ForegroundColor Yellow
Write-Host "Kapatmak icin acilan iki pencereyi de kapatin (veya Ctrl+C)." -ForegroundColor DarkGray
